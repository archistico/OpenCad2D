using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates native tangent fillets between lines and supported straight polyline segments.
/// </summary>
public sealed class FilletTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, ISnapModeProvider, IToolPreviewDescriptorProvider
{
    private const double MinimumPracticalFilletAngleRadians = 1e-6;

    private double _radius;
    private bool _trimEnabled = true;
    private FilletPick? _firstPick;
    private string? _feedback;
    private IReadOnlyList<CadEntity> _previewEntities = Array.Empty<CadEntity>();

    public string Name => "Fillet";

    public bool IsCommandFinished { get; private set; }

    public FilletToolState State { get; private set; } = FilletToolState.WaitingForFirstEntityOrRadius;

    public double Radius => _radius;

    public bool TrimEnabled => _trimEnabled;

    public EntityId? FirstEntityId => _firstPick?.EntityId;

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State is FilletToolState.WaitingForFirstEntityOrRadius or FilletToolState.WaitingForSecondEntity
            ? SnapKind.EntityOnly
            : SnapKind.None;
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureFirstPickIsCurrent(context);
        return new ToolPreviewDescriptor(
            entities: GetPreviewEntities(),
            entityOverlays: GetSelectedEntityOverlays());
    }

    private IReadOnlyList<ToolPreviewEntityOverlay> GetSelectedEntityOverlays()
    {
        if (_firstPick is null)
        {
            return Array.Empty<ToolPreviewEntityOverlay>();
        }

        CadEntity selectedGeometry = _firstPick.Entity is PolylineEntity polyline && _firstPick.PolylineSegmentIndex is { } segmentIndex
            ? CreateLineFromPolylineSegment(polyline, segmentIndex)
            : _firstPick.Entity;
        return new[]
        {
            new ToolPreviewEntityOverlay(
                new[] { selectedGeometry },
                ToolPreviewHighlightKind.Emphasis)
        };
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureFirstPickIsCurrent(context);
        return GetPreviewEntities();
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewEntities;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureFirstPickIsCurrent(context);
        var selectionOptions = new List<CommandOption>
        {
            new("Radius", "R", "Set fillet radius without losing the first selection"),
            new("Trim", "T", "Choose Trim or NoTrim")
        };
        if (_firstPick is not null)
        {
            selectionOptions.Add(new CommandOption("First", "F", "Choose the first object again"));
        }

        string settings = $"R={_radius:G}, {FormatTrimMode()}";
        return State switch
        {
            FilletToolState.WaitingForFirstEntityOrRadius => new CommandPromptState(
                "FILLET",
                $"1/2: Pick first line or straight polyline segment on the side to KEEP ({settings}). R sets radius; T sets mode; Enter finishes. Radius 0 joins line endpoints.",
                CommandInputKind.SelectionOrOption,
                selectionOptions,
                acceptsEmptyEnter: true,
                placeholder: "Click first object or choose R/T; Enter finishes",
                feedback: _feedback),

            FilletToolState.WaitingForRadius => new CommandPromptState(
                "FILLET", $"Specify fillet radius <{_radius:G}> (0 = sharp corner)",
                CommandInputKind.Number, acceptsEmptyEnter: true,
                placeholder: "Non-negative radius; Enter keeps current value",
                feedback: _feedback),

            FilletToolState.WaitingForTrimMode => new CommandPromptState(
                "FILLET", $"Choose mode <{FormatTrimMode()}>: Trim changes source objects; NoTrim adds only the arc.",
                CommandInputKind.Option,
                new[]
                {
                    new CommandOption("Trim", "T", "Trim or extend source objects"),
                    new CommandOption("NoTrim", "N", "Keep source objects and add only the fillet arc")
                },
                acceptsEmptyEnter: true, placeholder: "T = Trim, N = NoTrim; Enter keeps current mode",
                feedback: _feedback),

            FilletToolState.WaitingForSecondEntity => new CommandPromptState(
                "FILLET",
                $"2/2: Select second object on the sides to KEEP ({settings}). Check the preview; R/T changes settings; F changes first object; Enter finishes.",
                CommandInputKind.SelectionOrOption, selectionOptions,
                acceptsEmptyEnter: true,
                placeholder: "Click second line or adjacent straight segment; R/T/F recovers selection",
                feedback: _feedback),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ToolResult result = HandleCommandInputCore(input, context);
        _feedback = result.Message;
        if (result.Kind == ToolResultKind.None)
        {
            _previewEntities = Array.Empty<CadEntity>();
        }
        return result;
    }

    private ToolResult HandleCommandInputCore(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        EnsureFirstPickIsCurrent(context);
        if ((State is FilletToolState.WaitingForFirstEntityOrRadius or FilletToolState.WaitingForSecondEntity) &&
            input.Kind == CommandInputSubmissionKind.Confirm)
        {
            Reset(context);
            IsCommandFinished = true;
            return ToolResult.Completed("Fillet command finished.");
        }
        if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "First", StringComparison.OrdinalIgnoreCase))
        {
            Reset(context);
            return ToolResult.Started("Choose the first fillet object again. Radius and Trim/NoTrim mode are kept.");
        }

        if ((State is FilletToolState.WaitingForFirstEntityOrRadius or FilletToolState.WaitingForSecondEntity) &&
            input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Radius", StringComparison.OrdinalIgnoreCase))
        {
            State = FilletToolState.WaitingForRadius;
            _previewEntities = Array.Empty<CadEntity>();
            context.CurrentBasePoint = null;
            return ToolResult.Started("Specify fillet radius.");
        }

        if ((State is FilletToolState.WaitingForFirstEntityOrRadius or FilletToolState.WaitingForSecondEntity) &&
            input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Trim", StringComparison.OrdinalIgnoreCase))
        {
            State = FilletToolState.WaitingForTrimMode;
            _previewEntities = Array.Empty<CadEntity>();
            context.CurrentBasePoint = null;
            return ToolResult.Started($"Specify trim mode <{FormatTrimMode()}>.");
        }

        if (State == FilletToolState.WaitingForRadius)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                RestoreSelectionPhase(context);
                return ToolResult.Started($"Fillet radius remains {_radius:G}. {GetSelectionInstruction()}");
            }

            if (input.Kind == CommandInputSubmissionKind.Number &&
                input.Number is not null)
            {
                return AcceptRadius(context, input.Number.Value);
            }
        }

        if (State == FilletToolState.WaitingForTrimMode)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                RestoreSelectionPhase(context);
                return ToolResult.Started($"Fillet trim mode remains {FormatTrimMode()}. {GetSelectionInstruction()}");
            }

            if (input.Kind == CommandInputSubmissionKind.Option && input.OptionKeyword is not null)
            {
                return AcceptTrimMode(context, input.OptionKeyword);
            }
        }

        return State switch
        {
            FilletToolState.WaitingForFirstEntityOrRadius => ToolResult.None("Select the first line from the drawing canvas or type Radius."),
            FilletToolState.WaitingForRadius => ToolResult.None("Specify a non-negative fillet radius."),
            FilletToolState.WaitingForTrimMode => ToolResult.None("Specify Trim or NoTrim."),
            FilletToolState.WaitingForSecondEntity => ToolResult.None("Select the second line from the drawing canvas."),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ToolResult result = OnPointerPressedCore(context, pointer);
        _feedback = result.Message;
        if (result.Kind == ToolResultKind.None)
        {
            _previewEntities = Array.Empty<CadEntity>();
        }
        return result;
    }

    private ToolResult OnPointerPressedCore(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (!EnsureFirstPickIsCurrent(context))
        {
            return ToolResult.None(_feedback);
        }

        return State switch
        {
            FilletToolState.WaitingForFirstEntityOrRadius => AcceptFirstLine(context, pointer.ModelPoint),
            FilletToolState.WaitingForRadius => ToolResult.None("Type the fillet radius in the command input."),
            FilletToolState.WaitingForTrimMode => ToolResult.None("Type Trim or NoTrim in the command input."),
            FilletToolState.WaitingForSecondEntity => AcceptSecondLine(context, pointer.ModelPoint),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (!EnsureFirstPickIsCurrent(context))
        {
            return ToolResult.None(_feedback);
        }
        if (State != FilletToolState.WaitingForSecondEntity || _firstPick is null)
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        FilletPick? secondPick = PickSelectableFilletObject(context, pointer.ModelPoint, _firstPick);
        if (secondPick is null)
        {
            _previewEntities = Array.Empty<CadEntity>();
            _feedback = "Pick a different line or an adjacent straight polyline segment near the cursor. F chooses the first object again.";
            return ToolResult.None(_feedback);
        }

        if (!TryCreateFilletResult(_firstPick, secondPick, _radius, _trimEnabled, context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities, out string? errorMessage))
        {
            _previewEntities = Array.Empty<CadEntity>();
            _feedback = errorMessage;
            return ToolResult.None(errorMessage);
        }

        _previewEntities = resultEntities ?? Array.Empty<CadEntity>();
        _feedback = "Click to apply this fillet. R changes radius; T changes Trim/NoTrim; F changes the first object.";
        return ToolResult.Updated(_feedback);
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.Cancelled("Fillet command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.None("Fillet tool deactivated.");
    }

    private ToolResult AcceptRadius(
        ToolContext context,
        double radius)
    {
        if (!double.IsFinite(radius) || radius < 0)
        {
            return ToolResult.None("Fillet radius must be finite and non-negative.");
        }

        _radius = radius;
        RestoreSelectionPhase(context);

        return ToolResult.Started($"Fillet radius set to {_radius:G}. {GetSelectionInstruction()}");
    }

    private ToolResult AcceptTrimMode(
        ToolContext context,
        string optionKeyword)
    {
        if (string.Equals(optionKeyword, "Trim", StringComparison.OrdinalIgnoreCase))
        {
            _trimEnabled = true;
        }
        else if (string.Equals(optionKeyword, "NoTrim", StringComparison.OrdinalIgnoreCase))
        {
            _trimEnabled = false;
        }
        else
        {
            return ToolResult.None("Specify Trim or NoTrim.");
        }

        RestoreSelectionPhase(context);

        return ToolResult.Started($"Fillet trim mode set to {FormatTrimMode()}. {GetSelectionInstruction()}");
    }

    private ToolResult AcceptFirstLine(
        ToolContext context,
        Point2D pickPoint)
    {
        FilletPick? pick = PickSelectableFilletObject(context, pickPoint);

        if (pick is null)
        {
            return ToolResult.None("Fillet currently supports visible, unlocked lines and linear polyline segments only. Select the first object.");
        }

        if (pick.Entity is PolylineEntity { HasArcSegments: true })
        {
            return ToolResult.None("Polyline segment fillet currently supports linear polylines only.");
        }

        _firstPick = pick;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForSecondEntity;
        context.CurrentBasePoint = pick.ClosestPoint;

        return ToolResult.Started("First fillet object selected. Select second line or adjacent polyline segment.");
    }

    private ToolResult AcceptSecondLine(
        ToolContext context,
        Point2D pickPoint)
    {
        if (_firstPick is null)
        {
            State = FilletToolState.WaitingForFirstEntityOrRadius;
            return ToolResult.None("Select first line before selecting the second line.");
        }

        FilletPick? secondPick = PickSelectableFilletObject(context, pickPoint, _firstPick);

        if (secondPick is null)
        {
            return ToolResult.None("Select a visible, unlocked line or linear polyline segment as second fillet object.");
        }

        if (!TryCreateFilletResult(
                _firstPick,
                secondPick,
                _radius,
                _trimEnabled,
                context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities,
                out string? errorMessage))
        {
            return ToolResult.None(errorMessage ?? "Cannot create fillet for the selected objects.");
        }

        if (resultEntities is null)
        {
            return ToolResult.None(errorMessage ?? "Cannot create fillet for the selected objects.");
        }

        ICadCommand command = _trimEnabled
            ? new ModifyEntitiesCommand(
                GetRemovedEntitiesForFillet(_firstPick, secondPick),
                resultEntities,
                _radius <= context.GeometryTolerance.Distance
                    ? "Fillet with zero radius"
                    : "Fillet objects")
            : new AddEntityCommand(
                resultEntities);

        context.Commands.Execute(
            context.Document,
            command);

        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;

        return ToolResult.Completed("Fillet created. Select first line or polyline segment, or type Radius/Trim.");
    }

    private static bool TryCreateFilletResult(
        FilletPick firstPick,
        FilletPick secondPick,
        double radius,
        bool trimSourceLines,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (firstPick.Entity is LineEntity firstLine && secondPick.Entity is LineEntity secondLine)
        {
            if (secondPick.EntityId.Equals(firstPick.EntityId))
            {
                errorMessage = "Second fillet object must be different from the first one.";
                return false;
            }

            return TryCreateLineLineFillet(
                firstLine,
                firstPick.PickPoint,
                secondLine,
                secondPick.PickPoint,
                radius,
                trimSourceLines,
                tolerance,
                out resultEntities,
                out errorMessage);
        }

        if (firstPick.Entity is PolylineEntity firstPolyline &&
            secondPick.Entity is PolylineEntity secondPolyline &&
            firstPick.EntityId.Equals(secondPick.EntityId))
        {
            return TryCreatePolylineSegmentFillet(
                firstPolyline,
                firstPick.PolylineSegmentIndex,
                secondPick.PolylineSegmentIndex,
                radius,
                trimSourceLines,
                tolerance,
                out resultEntities,
                out errorMessage);
        }

        if ((firstPick.Entity is LineEntity || firstPick.Entity is PolylineEntity) &&
            (secondPick.Entity is LineEntity || secondPick.Entity is PolylineEntity))
        {
            return TryCreateSeparateObjectFillet(
                firstPick,
                secondPick,
                radius,
                trimSourceLines,
                tolerance,
                out resultEntities,
                out errorMessage);
        }

        errorMessage = "Polyline segment fillet currently supports lines, two adjacent segments of the same linear polyline, terminal segments of separate linear polylines, or line plus terminal polyline segment.";
        return false;
    }

    private static IReadOnlyList<CadEntity> GetRemovedEntitiesForFillet(
        FilletPick firstPick,
        FilletPick secondPick)
    {
        if (firstPick.EntityId.Equals(secondPick.EntityId))
        {
            return new[] { firstPick.Entity };
        }

        return new[] { firstPick.Entity, secondPick.Entity };
    }


    private static bool TryCreateSeparateObjectFillet(
        FilletPick firstPick,
        FilletPick secondPick,
        double radius,
        bool trimSourceLines,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (firstPick.EntityId.Equals(secondPick.EntityId))
        {
            errorMessage = "Second fillet object must be different from the first one.";
            return false;
        }

        if (!TryCreateFilletLineSource(firstPick, out LineEntity? firstLine, out FilletSourceKind firstKind, out errorMessage) ||
            firstLine is null)
        {
            return false;
        }

        if (!TryCreateFilletLineSource(secondPick, out LineEntity? secondLine, out FilletSourceKind secondKind, out errorMessage) ||
            secondLine is null)
        {
            return false;
        }

        if (!TryCreateLineLineFillet(
                firstLine,
                firstPick.PickPoint,
                secondLine,
                secondPick.PickPoint,
                radius,
                trimSourceLines,
                tolerance,
                out IReadOnlyList<CadEntity>? lineResultEntities,
                out errorMessage))
        {
            return false;
        }

        if (lineResultEntities is null)
        {
            errorMessage ??= "Cannot create fillet for the selected objects.";
            return false;
        }

        if (!trimSourceLines)
        {
            resultEntities = lineResultEntities;
            return true;
        }

        if (lineResultEntities.Count < 2 ||
            lineResultEntities[0] is not LineEntity firstTrimmed ||
            lineResultEntities[1] is not LineEntity secondTrimmed)
        {
            errorMessage = "Cannot create fillet geometry for the selected objects.";
            return false;
        }

        if (!TryConvertTrimmedFilletSource(firstTrimmed, firstPick, firstKind, tolerance, out CadEntity? convertedFirst, out errorMessage) ||
            convertedFirst is null)
        {
            return false;
        }

        if (!TryConvertTrimmedFilletSource(secondTrimmed, secondPick, secondKind, tolerance, out CadEntity? convertedSecond, out errorMessage) ||
            convertedSecond is null)
        {
            return false;
        }

        var converted = new List<CadEntity>
        {
            convertedFirst,
            convertedSecond
        };

        foreach (CadEntity entity in lineResultEntities.Skip(2))
        {
            converted.Add(entity);
        }

        resultEntities = converted;
        return true;
    }

    private static bool TryCreateFilletLineSource(
        FilletPick pick,
        out LineEntity? line,
        out FilletSourceKind kind,
        out string? errorMessage)
    {
        line = null;
        kind = FilletSourceKind.Line;
        errorMessage = null;

        if (pick.Entity is LineEntity lineEntity)
        {
            line = lineEntity;
            kind = FilletSourceKind.Line;
            return true;
        }

        if (pick.Entity is not PolylineEntity polyline)
        {
            errorMessage = "Fillet currently supports visible, unlocked lines and polyline segments only.";
            return false;
        }

        kind = FilletSourceKind.PolylineSegment;

        if (pick.PolylineSegmentIndex is null)
        {
            errorMessage = "Select a polyline segment.";
            return false;
        }

        if (polyline.IsClosed)
        {
            errorMessage = "Fillet between separate objects currently supports open polylines only.";
            return false;
        }

        if (polyline.HasArcSegments)
        {
            errorMessage = "Polyline segment fillet currently supports linear polylines only.";
            return false;
        }

        if (!IsTerminalOpenPolylineSegment(polyline, pick.PolylineSegmentIndex.Value))
        {
            errorMessage = "Fillet between separate polylines currently supports terminal segments only.";
            return false;
        }

        line = CreateLineFromPolylineSegment(polyline, pick.PolylineSegmentIndex.Value);
        return true;
    }

    private static bool TryConvertTrimmedFilletSource(
        LineEntity trimmedLine,
        FilletPick originalPick,
        FilletSourceKind kind,
        GeometryTolerance tolerance,
        out CadEntity? converted,
        out string? errorMessage)
    {
        converted = null;
        errorMessage = null;

        if (kind == FilletSourceKind.Line)
        {
            converted = trimmedLine;
            return true;
        }

        if (originalPick.Entity is not PolylineEntity sourcePolyline ||
            originalPick.PolylineSegmentIndex is null)
        {
            errorMessage = "Cannot update the selected polyline segment.";
            return false;
        }

        return TryReplaceTrimmedTerminalPolylineSegment(
            sourcePolyline,
            originalPick.PolylineSegmentIndex.Value,
            trimmedLine,
            tolerance,
            out converted,
            out errorMessage);
    }

    private static bool TryReplaceTrimmedTerminalPolylineSegment(
        PolylineEntity sourcePolyline,
        int segmentIndex,
        LineEntity trimmedLine,
        GeometryTolerance tolerance,
        out CadEntity? converted,
        out string? errorMessage)
    {
        converted = null;
        errorMessage = null;

        if (!IsTerminalOpenPolylineSegment(sourcePolyline, segmentIndex))
        {
            errorMessage = "Fillet between separate polylines currently supports terminal segments only.";
            return false;
        }

        Point2D originalStart = sourcePolyline.Vertices[segmentIndex];
        Point2D originalEnd = sourcePolyline.Vertices[segmentIndex + 1];
        bool startChanged = !tolerance.ArePointsEqual(trimmedLine.Start, originalStart);
        bool endChanged = !tolerance.ArePointsEqual(trimmedLine.End, originalEnd);

        if (startChanged && endChanged)
        {
            errorMessage = "Cannot trim the selected polyline segment without changing both endpoints.";
            return false;
        }

        var vertices = sourcePolyline.Vertices.ToList();

        if (sourcePolyline.SegmentCount == 1)
        {
            vertices[0] = trimmedLine.Start;
            vertices[1] = trimmedLine.End;
        }
        else if (segmentIndex == 0)
        {
            if (endChanged)
            {
                errorMessage = "Fillet between separate polylines can only trim the terminal endpoint of a multi-segment polyline.";
                return false;
            }

            vertices[0] = trimmedLine.Start;
        }
        else if (segmentIndex == sourcePolyline.SegmentCount - 1)
        {
            if (startChanged)
            {
                errorMessage = "Fillet between separate polylines can only trim the terminal endpoint of a multi-segment polyline.";
                return false;
            }

            vertices[^1] = trimmedLine.End;
        }
        else
        {
            errorMessage = "Fillet between separate polylines currently supports terminal segments only.";
            return false;
        }

        converted = new PolylineEntity(
            vertices,
            isClosed: false,
            layerId: sourcePolyline.LayerId,
            style: sourcePolyline.Style,
            isVisible: sourcePolyline.IsVisible,
            isLocked: sourcePolyline.IsLocked,
            drawOrder: sourcePolyline.DrawOrder,
            isFilled: false,
            segmentBulges: sourcePolyline.SegmentBulges);
        return true;
    }

    private static bool IsTerminalOpenPolylineSegment(
        PolylineEntity polyline,
        int segmentIndex)
    {
        return !polyline.IsClosed &&
            segmentIndex >= 0 &&
            segmentIndex < polyline.SegmentCount &&
            (segmentIndex == 0 || segmentIndex == polyline.SegmentCount - 1);
    }

    private static bool TryCreatePolylineSegmentFillet(
        PolylineEntity polyline,
        int? firstSegmentIndex,
        int? secondSegmentIndex,
        double radius,
        bool trimSourceLines,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (!trimSourceLines)
        {
            errorMessage = "Polyline segment fillet requires Trim mode.";
            return false;
        }

        if (radius <= tolerance.Distance)
        {
            errorMessage = "Polyline segment fillet requires a radius greater than zero.";
            return false;
        }

        if (firstSegmentIndex is null || secondSegmentIndex is null)
        {
            errorMessage = "Select two polyline segments.";
            return false;
        }

        if (polyline.HasArcSegments)
        {
            errorMessage = "Polyline segment fillet currently supports linear polylines only.";
            return false;
        }

        if (firstSegmentIndex.Value == secondSegmentIndex.Value)
        {
            errorMessage = "Select two different adjacent polyline segments.";
            return false;
        }

        if (!TryGetAdjacentPolylineSegments(
                polyline,
                firstSegmentIndex.Value,
                secondSegmentIndex.Value,
                out int beforeSegmentIndex,
                out int afterSegmentIndex,
                out int commonVertexIndex))
        {
            errorMessage = "Selected polyline segments are not adjacent.";
            return false;
        }

        int vertexCount = polyline.Vertices.Count;
        int previousVertexIndex = beforeSegmentIndex;
        int nextVertexIndex = (afterSegmentIndex + 1) % vertexCount;

        Point2D common = polyline.Vertices[commonVertexIndex];
        Point2D previous = polyline.Vertices[previousVertexIndex];
        Point2D next = polyline.Vertices[nextVertexIndex];

        Vector2D previousBranch = common.VectorTo(previous);
        Vector2D nextBranch = common.VectorTo(next);

        if (tolerance.IsVectorLengthZero(previousBranch.Length) ||
            tolerance.IsVectorLengthZero(nextBranch.Length))
        {
            errorMessage = "Cannot fillet zero-length polyline segments.";
            return false;
        }

        Vector2D previousUnit = previousBranch.Normalize();
        Vector2D nextUnit = nextBranch.Normalize();
        double angle = Math.Acos(Math.Clamp(previousUnit.Dot(nextUnit), -1.0, 1.0));
        double minimumAngle = Math.Max(tolerance.Angle, MinimumPracticalFilletAngleRadians);

        if (angle <= minimumAngle || Math.Abs(Math.PI - angle) <= minimumAngle)
        {
            errorMessage = "Cannot fillet polyline segments with an invalid or nearly collinear corner angle.";
            return false;
        }

        double tangentDistance = radius / Math.Tan(angle / 2.0);

        if (tangentDistance <= tolerance.Distance ||
            double.IsNaN(tangentDistance) ||
            double.IsInfinity(tangentDistance))
        {
            errorMessage = "Fillet radius is not valid for the selected polyline corner.";
            return false;
        }

        if (tangentDistance >= common.DistanceTo(previous) - tolerance.Distance ||
            tangentDistance >= common.DistanceTo(next) - tolerance.Distance)
        {
            errorMessage = "Fillet radius is too large for the selected polyline segments.";
            return false;
        }

        Point2D previousTangent = common + previousUnit * tangentDistance;
        Point2D nextTangent = common + nextUnit * tangentDistance;
        // The angle between the two polyline branches is the corner angle at the
        // original vertex.  The fillet arc itself spans the supplementary angle
        // between the two tangent radii; using the corner angle directly gives
        // the correct result only for 90° corners and produces a wrong radius
        // for acute/obtuse corners.
        double filletArcSweep = Math.PI - angle;
        double bulgeMagnitude = Math.Tan(filletArcSweep / 4.0);
        double cross = previousUnit.Cross(nextUnit);
        double arcBulge = cross < 0.0
            ? -bulgeMagnitude
            : bulgeMagnitude;

        IReadOnlyList<PolylineSegmentPiece> pieces = BuildFilletedPolylineSegments(
            polyline,
            beforeSegmentIndex,
            afterSegmentIndex,
            previousTangent,
            nextTangent,
            arcBulge,
            tolerance);

        if (pieces.Count == 0)
        {
            errorMessage = "Cannot create filleted polyline geometry.";
            return false;
        }

        var vertices = new List<Point2D>
        {
            pieces[0].Start
        };
        var bulges = new List<double>();

        for (int index = 0; index < pieces.Count; index++)
        {
            PolylineSegmentPiece piece = pieces[index];
            bulges.Add(piece.Bulge);
            bool closesPolyline = polyline.IsClosed &&
                index == pieces.Count - 1 &&
                tolerance.ArePointsEqual(piece.End, vertices[0]);

            if (!closesPolyline)
            {
                vertices.Add(piece.End);
            }
        }

        var result = new PolylineEntity(
            vertices,
            polyline.IsClosed,
            layerId: polyline.LayerId,
            style: polyline.Style,
            isVisible: polyline.IsVisible,
            isLocked: polyline.IsLocked,
            drawOrder: polyline.DrawOrder,
            isFilled: polyline.IsClosed && polyline.IsFilled,
            segmentBulges: bulges);

        resultEntities = new CadEntity[] { result };
        return true;
    }

    private static IReadOnlyList<PolylineSegmentPiece> BuildFilletedPolylineSegments(
        PolylineEntity polyline,
        int beforeSegmentIndex,
        int afterSegmentIndex,
        Point2D previousTangent,
        Point2D nextTangent,
        double arcBulge,
        GeometryTolerance tolerance)
    {
        var pieces = new List<PolylineSegmentPiece>();

        for (int index = 0; index < polyline.SegmentCount; index++)
        {
            Point2D start = polyline.Vertices[index];
            Point2D end = polyline.Vertices[(index + 1) % polyline.Vertices.Count];

            if (index == beforeSegmentIndex)
            {
                AddPolylinePieceIfLongEnough(pieces, start, previousTangent, 0.0, tolerance);

                if (afterSegmentIndex == beforeSegmentIndex + 1)
                {
                    AddPolylinePieceIfLongEnough(pieces, previousTangent, nextTangent, arcBulge, tolerance);
                }

                continue;
            }

            if (index == afterSegmentIndex)
            {
                AddPolylinePieceIfLongEnough(pieces, nextTangent, end, 0.0, tolerance);
                continue;
            }

            AddPolylinePieceIfLongEnough(pieces, start, end, 0.0, tolerance);
        }

        if (polyline.IsClosed && afterSegmentIndex == 0 && beforeSegmentIndex == polyline.SegmentCount - 1)
        {
            AddPolylinePieceIfLongEnough(pieces, previousTangent, nextTangent, arcBulge, tolerance);
        }

        return pieces;
    }

    private static void AddPolylinePieceIfLongEnough(
        List<PolylineSegmentPiece> pieces,
        Point2D start,
        Point2D end,
        double bulge,
        GeometryTolerance tolerance)
    {
        if (tolerance.ArePointsEqual(start, end))
        {
            return;
        }

        pieces.Add(new PolylineSegmentPiece(start, end, bulge));
    }

    private static bool TryGetAdjacentPolylineSegments(
        PolylineEntity polyline,
        int firstSegmentIndex,
        int secondSegmentIndex,
        out int beforeSegmentIndex,
        out int afterSegmentIndex,
        out int commonVertexIndex)
    {
        beforeSegmentIndex = -1;
        afterSegmentIndex = -1;
        commonVertexIndex = -1;

        if (firstSegmentIndex < 0 || firstSegmentIndex >= polyline.SegmentCount ||
            secondSegmentIndex < 0 || secondSegmentIndex >= polyline.SegmentCount)
        {
            return false;
        }

        int vertexCount = polyline.Vertices.Count;
        int firstEnd = (firstSegmentIndex + 1) % vertexCount;
        int secondEnd = (secondSegmentIndex + 1) % vertexCount;

        if (firstEnd == secondSegmentIndex)
        {
            beforeSegmentIndex = firstSegmentIndex;
            afterSegmentIndex = secondSegmentIndex;
            commonVertexIndex = firstEnd;
            return true;
        }

        if (secondEnd == firstSegmentIndex)
        {
            beforeSegmentIndex = secondSegmentIndex;
            afterSegmentIndex = firstSegmentIndex;
            commonVertexIndex = secondEnd;
            return true;
        }

        return false;
    }

    internal static bool TryCreateLineLineFillet(
        LineEntity first,
        Point2D firstPickPoint,
        LineEntity second,
        Point2D secondPickPoint,
        double radius,
        bool trimSourceLines,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (!double.IsFinite(radius) || radius < 0)
        {
            errorMessage = "Fillet radius must be finite and non-negative.";
            return false;
        }

        if (!LineIntersectionService.TryIntersectInfiniteLines(
                first.Geometry,
                second.Geometry,
                out LineIntersectionInfo intersection,
                tolerance))
        {
            errorMessage = "Cannot fillet parallel or coincident lines.";
            return false;
        }

        Vector2D firstDirection = first.Start.VectorTo(first.End);
        Vector2D secondDirection = second.Start.VectorTo(second.End);

        if (tolerance.IsVectorLengthZero(firstDirection.Length) ||
            tolerance.IsVectorLengthZero(secondDirection.Length))
        {
            errorMessage = "Cannot fillet zero-length lines.";
            return false;
        }

        Vector2D firstUnit = firstDirection.Normalize();
        Vector2D secondUnit = secondDirection.Normalize();
        Point2D intersectionPoint = intersection.Point;

        if (!TryGetKeptBranch(first, firstPickPoint, intersectionPoint, firstUnit, tolerance, out Vector2D firstBranch) ||
            !TryGetKeptBranch(second, secondPickPoint, intersectionPoint, secondUnit, tolerance, out Vector2D secondBranch))
        {
            errorMessage = "The pick is on an interior intersection. Pick each object on the side to KEEP, away from the crossing.";
            return false;
        }

        double branchDot = Math.Clamp(firstBranch.Dot(secondBranch), -1.0, 1.0);
        double angle = Math.Acos(branchDot);

        double minimumAngle = Math.Max(tolerance.Angle, MinimumPracticalFilletAngleRadians);

        if (angle <= minimumAngle || Math.Abs(Math.PI - angle) <= minimumAngle)
        {
            errorMessage = "Cannot fillet lines with an invalid or nearly collinear corner angle.";
            return false;
        }

        if (radius <= tolerance.Distance)
        {
            if (!trimSourceLines)
            {
                errorMessage = "Zero-radius fillet requires Trim mode because NoTrim would not create new geometry.";
                return false;
            }

            resultEntities = new CadEntity[]
            {
                CreateTrimmedLineToPoint(first, firstBranch, intersectionPoint),
                CreateTrimmedLineToPoint(second, secondBranch, intersectionPoint)
            };
            return true;
        }

        double tangentDistance = radius / Math.Tan(angle / 2.0);

        if (tangentDistance <= tolerance.Distance || double.IsNaN(tangentDistance) || double.IsInfinity(tangentDistance))
        {
            errorMessage = "Fillet radius is not valid for the selected angle.";
            return false;
        }

        double firstAvailable = Math.Max(intersectionPoint.VectorTo(first.Start).Dot(firstBranch), intersectionPoint.VectorTo(first.End).Dot(firstBranch));
        double secondAvailable = Math.Max(intersectionPoint.VectorTo(second.Start).Dot(secondBranch), intersectionPoint.VectorTo(second.End).Dot(secondBranch));
        if (trimSourceLines
                ? tangentDistance >= firstAvailable - tolerance.Distance || tangentDistance >= secondAvailable - tolerance.Distance
                : tangentDistance > firstAvailable + tolerance.Distance || tangentDistance > secondAvailable + tolerance.Distance)
        {
            errorMessage = "Fillet radius is too large for the selected sides. Use R to reduce the radius or F to choose the first object again.";
            return false;
        }

        Point2D firstTangent = intersectionPoint + firstBranch * tangentDistance;
        Point2D secondTangent = intersectionPoint + secondBranch * tangentDistance;
        Vector2D bisectorVector = firstBranch + secondBranch;

        if (tolerance.IsVectorLengthZero(bisectorVector.Length))
        {
            errorMessage = "Cannot fillet lines with a degenerate corner bisector.";
            return false;
        }

        Vector2D bisector = bisectorVector.Normalize();
        Point2D center = intersectionPoint + bisector * (radius / Math.Sin(angle / 2.0));

        if (!double.IsFinite(center.X) || !double.IsFinite(center.Y) ||
            !double.IsFinite(firstTangent.X) || !double.IsFinite(firstTangent.Y) ||
            !double.IsFinite(secondTangent.X) || !double.IsFinite(secondTangent.Y))
        {
            errorMessage = "Fillet geometry exceeds the supported numeric range. Reduce the radius.";
            return false;
        }

        Angle startAngle = Angle.FromRadians(
            Math.Atan2(
                firstTangent.Y - center.Y,
                firstTangent.X - center.X));
        Angle endAngle = Angle.FromRadians(
            Math.Atan2(
                secondTangent.Y - center.Y,
                secondTangent.X - center.X));
        bool isCounterClockwise = center.VectorTo(firstTangent).Cross(center.VectorTo(secondTangent)) > 0;

        var filletArc = new ArcEntity(
            center,
            radius,
            startAngle,
            endAngle,
            isCounterClockwise,
            layerId: first.LayerId,
            style: first.Style,
            isVisible: first.IsVisible,
            isLocked: first.IsLocked,
            drawOrder: Math.Max(first.DrawOrder, second.DrawOrder) + 1);

        resultEntities = trimSourceLines
            ? new CadEntity[]
            {
                CreateTrimmedLineToPoint(first, firstBranch, firstTangent),
                CreateTrimmedLineToPoint(second, secondBranch, secondTangent),
                filletArc
            }
            : new CadEntity[]
            {
                filletArc
            };
        return true;
    }

    private static LineEntity CreateLineFromPolylineSegment(
        PolylineEntity polyline,
        int segmentIndex)
    {
        return new LineEntity(
            polyline.Vertices[segmentIndex],
            polyline.Vertices[(segmentIndex + 1) % polyline.Vertices.Count],
            layerId: polyline.LayerId,
            style: polyline.Style,
            isVisible: polyline.IsVisible,
            isLocked: polyline.IsLocked,
            drawOrder: polyline.DrawOrder);
    }

    private static LineEntity CreateTrimmedLineToPoint(
        LineEntity source,
        Vector2D keptBranch,
        Point2D endPoint)
    {
        Vector2D sourceDirection = source.Start.VectorTo(source.End).Normalize();
        bool keepStartSide = keptBranch.Dot(sourceDirection) < 0;

        return keepStartSide
            ? new LineEntity(
                source.Start,
                endPoint,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
            : new LineEntity(
                endPoint,
                source.End,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder);
    }

    private static bool TryGetKeptBranch(
        LineEntity line,
        Point2D pickPoint,
        Point2D intersection,
        Vector2D unit,
        GeometryTolerance tolerance,
        out Vector2D branch)
    {
        double pickDistance = intersection.VectorTo(pickPoint).Dot(unit);
        if (Math.Abs(pickDistance) <= tolerance.Distance)
        {
            double startDistance = intersection.VectorTo(line.Start).Dot(unit);
            double endDistance = intersection.VectorTo(line.End).Dot(unit);
            if (startDistance < -tolerance.Distance && endDistance > tolerance.Distance)
            {
                branch = default;
                return false;
            }

            // At a terminal intersection only the ray containing the source is available.
            pickDistance = Math.Abs(startDistance) > Math.Abs(endDistance) ? startDistance : endDistance;
        }

        branch = pickDistance < 0 ? unit * -1.0 : unit;
        return true;
    }

    private static FilletPick? PickSelectableFilletObject(
        ToolContext context,
        Point2D pickPoint,
        FilletPick? firstPick = null)
    {
        IReadOnlyList<EntityId> selectedIds = context.Selection.Service.SelectAllByPoint(
            context.Document,
            pickPoint,
            context.Selection.Tolerance);

        foreach (EntityId selectedId in selectedIds)
        {
            CadEntity entity = context.Document.Entities.GetRequired(selectedId);

            if (!context.Document.IsEntitySelectable(entity))
            {
                continue;
            }

            if (entity is LineEntity)
            {
                if (firstPick is not null && selectedId.Equals(firstPick.EntityId))
                {
                    continue;
                }

                return new FilletPick(
                    selectedId,
                    pickPoint,
                    entity.GetClosestPoint(pickPoint),
                    entity,
                    null);
            }

            if (entity is PolylineEntity polyline)
            {
                int? excludedSegmentIndex = firstPick is not null &&
                    selectedId.Equals(firstPick.EntityId)
                        ? firstPick.PolylineSegmentIndex
                        : null;

                int? segmentIndex = FindClosestPolylineSegment(
                    polyline,
                    pickPoint,
                    excludedSegmentIndex);

                if (segmentIndex is null)
                {
                    continue;
                }

                Point2D closestPoint = GetClosestPointOnSegment(
                    polyline.Vertices[segmentIndex.Value],
                    polyline.Vertices[(segmentIndex.Value + 1) % polyline.Vertices.Count], pickPoint);
                if (closestPoint.DistanceTo(pickPoint) > context.Selection.Tolerance)
                {
                    continue;
                }

                return new FilletPick(
                    selectedId,
                    pickPoint,
                    closestPoint,
                    entity,
                    segmentIndex.Value);
            }
        }

        return null;
    }

    private static int? FindClosestPolylineSegment(
        PolylineEntity polyline,
        Point2D pickPoint,
        int? excludedSegmentIndex = null)
    {
        if (polyline.SegmentCount == 0)
        {
            return null;
        }

        double bestDistance = double.PositiveInfinity;
        int bestIndex = -1;

        for (int index = 0; index < polyline.SegmentCount; index++)
        {
            if (excludedSegmentIndex is not null && index == excludedSegmentIndex.Value)
            {
                continue;
            }

            Point2D start = polyline.Vertices[index];
            Point2D end = polyline.Vertices[(index + 1) % polyline.Vertices.Count];
            Point2D closest = GetClosestPointOnSegment(start, end, pickPoint);
            double distance = closest.DistanceTo(pickPoint);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        return bestIndex >= 0
            ? bestIndex
            : null;
    }

    private static Point2D GetClosestPointOnSegment(
        Point2D start,
        Point2D end,
        Point2D point)
    {
        return DistanceService.ClosestPointOnSegment(point, new LineSegment2D(start, end));
    }

    private sealed record FilletPick(
        EntityId EntityId,
        Point2D PickPoint,
        Point2D ClosestPoint,
        CadEntity Entity,
        int? PolylineSegmentIndex);

    private readonly record struct PolylineSegmentPiece(
        Point2D Start,
        Point2D End,
        double Bulge);

    private enum FilletSourceKind
    {
        Line,
        PolylineSegment
    }

    private bool EnsureFirstPickIsCurrent(ToolContext context)
    {
        if (_firstPick is null)
        {
            return true;
        }
        if (context.Document.Entities.TryGet(_firstPick.EntityId, out CadEntity? current) &&
            ReferenceEquals(current, _firstPick.Entity) && context.Document.IsEntitySelectable(current))
        {
            return true;
        }

        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        context.CurrentBasePoint = null;
        if (State == FilletToolState.WaitingForSecondEntity)
        {
            State = FilletToolState.WaitingForFirstEntityOrRadius;
        }
        _feedback = "The first object changed, was removed, hidden or locked. Select the first object again; radius and mode are kept.";
        return false;
    }

    private void RestoreSelectionPhase(ToolContext context)
    {
        EnsureFirstPickIsCurrent(context);
        State = _firstPick is null ? FilletToolState.WaitingForFirstEntityOrRadius : FilletToolState.WaitingForSecondEntity;
        context.CurrentBasePoint = _firstPick?.ClosestPoint;
        _previewEntities = Array.Empty<CadEntity>();
    }

    private string FormatTrimMode()
    {
        return _trimEnabled ? "Trim" : "NoTrim";
    }

    private string GetSelectionInstruction()
    {
        return _firstPick is null
            ? "Select the first object on the side to KEEP."
            : "Select the second object on the side to KEEP; F changes the first object.";
    }

    private void Reset(ToolContext context)
    {
        IsCommandFinished = false;
        _feedback = null;
        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;
    }
}
