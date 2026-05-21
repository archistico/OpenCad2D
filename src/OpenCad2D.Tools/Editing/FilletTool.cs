using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates a tangent fillet between two lines.
/// v0.8 supports Line-Line with Radius and Radius=0 corner joining.
/// </summary>
public sealed class FilletTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, ISnapModeProvider, IToolPreviewDescriptorProvider
{
    private const double MinimumPracticalFilletAngleRadians = 1e-6;

    private double _radius;
    private bool _trimEnabled = true;
    private ToolPickedEntityInput? _firstPick;
    private IReadOnlyList<CadEntity> _previewEntities = Array.Empty<CadEntity>();

    public string Name => "Fillet";

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

        return new[]
        {
            new ToolPreviewEntityOverlay(
                new[] { _firstPick.Entity },
                ToolPreviewHighlightKind.Emphasis)
        };
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetPreviewEntities();
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewEntities;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            FilletToolState.WaitingForFirstEntityOrRadius => new CommandPromptState(
                "FILLET",
                $"Select first line or [Radius/Trim] <{_radius:0.###}> ({FormatTrimMode()})",
                CommandInputKind.SelectionOrOption,
                new[]
                {
                    new CommandOption("Radius", "R", "Set fillet radius"),
                    new CommandOption("Trim", "T", "Set whether source lines are trimmed")
                },
                placeholder: "Click first line or type R/T"),

            FilletToolState.WaitingForRadius => new CommandPromptState(
                "FILLET",
                $"Specify fillet radius <{_radius:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Radius, for example 10 or 0"),

            FilletToolState.WaitingForTrimMode => new CommandPromptState(
                "FILLET",
                $"Specify trim mode <{FormatTrimMode()}>",
                CommandInputKind.Option,
                new[]
                {
                    new CommandOption("Trim", "T", "Trim source lines"),
                    new CommandOption("NoTrim", "N", "Keep source lines and add only the fillet arc")
                },
                acceptsEmptyEnter: true,
                placeholder: "Trim or NoTrim"),

            FilletToolState.WaitingForSecondEntity => new CommandPromptState(
                "FILLET",
                "Select second line",
                CommandInputKind.Selection,
                placeholder: "Click second line"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == FilletToolState.WaitingForFirstEntityOrRadius &&
            input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Radius", StringComparison.OrdinalIgnoreCase))
        {
            State = FilletToolState.WaitingForRadius;
            context.CurrentBasePoint = null;
            return ToolResult.Started("Specify fillet radius.");
        }

        if (State == FilletToolState.WaitingForFirstEntityOrRadius &&
            input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Trim", StringComparison.OrdinalIgnoreCase))
        {
            State = FilletToolState.WaitingForTrimMode;
            context.CurrentBasePoint = null;
            return ToolResult.Started($"Specify trim mode <{FormatTrimMode()}>.");
        }

        if (State == FilletToolState.WaitingForRadius)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                State = FilletToolState.WaitingForFirstEntityOrRadius;
                context.CurrentBasePoint = null;
                return ToolResult.Started($"Fillet radius remains {_radius:0.###}. Select first line.");
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
                State = FilletToolState.WaitingForFirstEntityOrRadius;
                return ToolResult.Started($"Fillet trim mode remains {FormatTrimMode()}. Select first line.");
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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

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

        if (State != FilletToolState.WaitingForSecondEntity || _firstPick is null)
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        ToolPickedEntityInput? secondPick = PickSelectableLine(context, pointer.ModelPoint);

        if (secondPick is null || secondPick.EntityId.Equals(_firstPick.EntityId))
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        LineEntity first = (LineEntity)_firstPick.Entity;
        LineEntity second = (LineEntity)secondPick.Entity;

        if (!TryCreateLineLineFillet(
                first,
                _firstPick.PickPoint,
                second,
                secondPick.PickPoint,
                _radius,
                _trimEnabled,
                context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities,
                out _))
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        _previewEntities = resultEntities ?? Array.Empty<CadEntity>();

        return _previewEntities.Count > 0
            ? ToolResult.Updated("Fillet preview updated.")
            : ToolResult.None();
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
        if (radius < 0)
        {
            return ToolResult.None("Fillet radius cannot be negative.");
        }

        _radius = radius;
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;

        return ToolResult.Started($"Fillet radius set to {_radius:0.###}. Select first line.");
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

        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;

        return ToolResult.Started($"Fillet trim mode set to {FormatTrimMode()}. Select first line.");
    }

    private ToolResult AcceptFirstLine(
        ToolContext context,
        Point2D pickPoint)
    {
        ToolPickedEntityInput? pick = PickSelectableLine(context, pickPoint);

        if (pick is null)
        {
            return ToolResult.None("Fillet currently supports visible, unlocked lines only. Select the first line.");
        }

        _firstPick = pick;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForSecondEntity;
        context.CurrentBasePoint = pick.ClosestPoint;

        return ToolResult.Started("First fillet object selected. Select second line.");
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

        ToolPickedEntityInput? secondPick = PickSelectableLine(context, pickPoint);

        if (secondPick is null)
        {
            return ToolResult.None("Select a visible, unlocked line as second fillet object.");
        }

        if (secondPick.EntityId.Equals(_firstPick.EntityId))
        {
            return ToolResult.None("Second fillet object must be different from the first one.");
        }

        LineEntity first = (LineEntity)_firstPick.Entity;
        LineEntity second = (LineEntity)secondPick.Entity;

        if (!TryCreateLineLineFillet(
                first,
                _firstPick.PickPoint,
                second,
                secondPick.PickPoint,
                _radius,
                _trimEnabled,
                context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities,
                out string? errorMessage))
        {
            return ToolResult.None(errorMessage ?? "Cannot create fillet for the selected lines.");
        }

        if (resultEntities is null)
        {
            return ToolResult.None(errorMessage ?? "Cannot create fillet for the selected lines.");
        }

        ICadCommand command = _trimEnabled
            ? new ModifyEntitiesCommand(
                new[] { first, second },
                resultEntities,
                _radius <= context.GeometryTolerance.Distance
                    ? "Fillet lines with zero radius"
                    : "Fillet lines")
            : new AddEntityCommand(
                resultEntities);

        context.Commands.Execute(
            context.Document,
            command);

        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;

        return ToolResult.Completed("Fillet created. Select first line or type Radius/Trim.");
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

        double firstPickParameter = GetLineParameter(first, firstPickPoint);
        double secondPickParameter = GetLineParameter(second, secondPickPoint);

        Vector2D firstBranch = firstPickParameter < intersection.FirstParameter
            ? firstUnit * -1.0
            : firstUnit;
        Vector2D secondBranch = secondPickParameter < intersection.SecondParameter
            ? secondUnit * -1.0
            : secondUnit;

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

    private static double GetLineParameter(
        LineEntity line,
        Point2D point)
    {
        // Intentionally not clamped to [0, 1]: fillet uses the infinite-line
        // parameter to decide which branch from the intersection point is being picked.
        Vector2D direction = line.Start.VectorTo(line.End);
        double lengthSquared = direction.LengthSquared;

        if (lengthSquared <= 0)
        {
            return 0;
        }

        return line.Start.VectorTo(point).Dot(direction) / lengthSquared;
    }

    private static ToolPickedEntityInput? PickSelectableLine(
        ToolContext context,
        Point2D pickPoint)
    {
        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pickPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return null;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (entity is not LineEntity || !context.Document.IsEntitySelectable(entity))
        {
            return null;
        }

        return new ToolPickedEntityInput(
            selectedId.Value,
            pickPoint,
            entity.GetClosestPoint(pickPoint),
            entity);
    }

    private string FormatTrimMode()
    {
        return _trimEnabled ? "Trim" : "NoTrim";
    }

    private void Reset(ToolContext context)
    {
        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;
    }
}
