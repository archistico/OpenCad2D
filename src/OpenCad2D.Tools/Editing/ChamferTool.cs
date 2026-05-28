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
/// Creates an equal-distance chamfer between two lines or two adjacent linear polyline segments.
/// </summary>
public sealed class ChamferTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, ISnapModeProvider, IToolPreviewDescriptorProvider
{
    private double _distance;
    private ChamferPick? _firstPick;
    private IReadOnlyList<CadEntity> _previewEntities = Array.Empty<CadEntity>();

    public string Name => "Chamfer";

    public ChamferToolState State { get; private set; } = ChamferToolState.WaitingForFirstEntityOrDistance;

    public double Distance => _distance;

    public EntityId? FirstEntityId => _firstPick?.EntityId;

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State is ChamferToolState.WaitingForFirstEntityOrDistance or ChamferToolState.WaitingForSecondEntity
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
            ChamferToolState.WaitingForFirstEntityOrDistance => new CommandPromptState(
                "CHAMFER",
                $"Select first line or polyline segment or [Distance] <{_distance:0.###}>",
                CommandInputKind.SelectionOrOption,
                new[]
                {
                    new CommandOption("Distance", "D", "Set equal chamfer distance")
                },
                placeholder: "Click first object or type D"),

            ChamferToolState.WaitingForDistance => new CommandPromptState(
                "CHAMFER",
                $"Specify chamfer distance <{_distance:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Distance, for example 10"),

            ChamferToolState.WaitingForSecondEntity => new CommandPromptState(
                "CHAMFER",
                "Select second line or adjacent polyline segment",
                CommandInputKind.Selection,
                placeholder: "Click second object"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == ChamferToolState.WaitingForFirstEntityOrDistance &&
            input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Distance", StringComparison.OrdinalIgnoreCase))
        {
            State = ChamferToolState.WaitingForDistance;
            context.CurrentBasePoint = null;
            return ToolResult.Started("Specify chamfer distance.");
        }

        if (State == ChamferToolState.WaitingForDistance)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                State = ChamferToolState.WaitingForFirstEntityOrDistance;
                context.CurrentBasePoint = null;
                return ToolResult.Started($"Chamfer distance remains {_distance:0.###}. Select first line or polyline segment.");
            }

            if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
            {
                return AcceptDistance(context, input.Number.Value);
            }
        }

        return State switch
        {
            ChamferToolState.WaitingForFirstEntityOrDistance => ToolResult.None("Select the first line or polyline segment from the drawing canvas or type Distance."),
            ChamferToolState.WaitingForDistance => ToolResult.None("Specify a non-negative chamfer distance."),
            ChamferToolState.WaitingForSecondEntity => ToolResult.None("Select the second line or adjacent polyline segment from the drawing canvas."),
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
            ChamferToolState.WaitingForFirstEntityOrDistance => AcceptFirstObject(context, pointer.ModelPoint),
            ChamferToolState.WaitingForDistance => ToolResult.None("Type the chamfer distance in the command input."),
            ChamferToolState.WaitingForSecondEntity => AcceptSecondObject(context, pointer.ModelPoint),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != ChamferToolState.WaitingForSecondEntity || _firstPick is null)
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        ChamferPick? secondPick = PickSelectableChamferObject(context, pointer.ModelPoint, _firstPick);
        if (secondPick is null)
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        if (!TryCreateChamferResult(
                _firstPick,
                secondPick,
                _distance,
                context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities,
                out _))
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        _previewEntities = resultEntities ?? Array.Empty<CadEntity>();

        return _previewEntities.Count > 0
            ? ToolResult.Updated("Chamfer preview updated.")
            : ToolResult.None();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.Cancelled("Chamfer command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.None("Chamfer tool deactivated.");
    }

    private ToolResult AcceptDistance(
        ToolContext context,
        double distance)
    {
        if (distance < 0)
        {
            return ToolResult.None("Chamfer distance cannot be negative.");
        }

        _distance = distance;
        State = ChamferToolState.WaitingForFirstEntityOrDistance;
        context.CurrentBasePoint = null;

        return ToolResult.Started($"Chamfer distance set to {_distance:0.###}. Select first line or polyline segment.");
    }

    private ToolResult AcceptFirstObject(
        ToolContext context,
        Point2D pickPoint)
    {
        ChamferPick? pick = PickSelectableChamferObject(context, pickPoint);
        if (pick is null)
        {
            return ToolResult.None("Chamfer currently supports visible, unlocked lines and linear polyline segments only. Select the first object.");
        }

        _firstPick = pick;
        _previewEntities = Array.Empty<CadEntity>();
        State = ChamferToolState.WaitingForSecondEntity;
        context.CurrentBasePoint = pick.ClosestPoint;

        return ToolResult.Started("First chamfer object selected. Select second line or adjacent polyline segment.");
    }

    private ToolResult AcceptSecondObject(
        ToolContext context,
        Point2D pickPoint)
    {
        if (_firstPick is null)
        {
            State = ChamferToolState.WaitingForFirstEntityOrDistance;
            return ToolResult.None("Select first object before selecting the second object.");
        }

        ChamferPick? secondPick = PickSelectableChamferObject(context, pickPoint, _firstPick);
        if (secondPick is null)
        {
            return ToolResult.None("Select a visible, unlocked line or linear polyline segment as second chamfer object.");
        }

        if (!TryCreateChamferResult(
                _firstPick,
                secondPick,
                _distance,
                context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities,
                out string? errorMessage))
        {
            return ToolResult.None(errorMessage ?? "Cannot create chamfer for the selected objects.");
        }

        if (resultEntities is null)
        {
            return ToolResult.None(errorMessage ?? "Cannot create chamfer for the selected objects.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                GetRemovedEntitiesForChamfer(_firstPick, secondPick),
                resultEntities,
                "Chamfer objects"));

        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = ChamferToolState.WaitingForFirstEntityOrDistance;
        context.CurrentBasePoint = null;

        return ToolResult.Completed("Chamfer created. Select first line or polyline segment, or type Distance.");
    }

    private static bool TryCreateChamferResult(
        ChamferPick firstPick,
        ChamferPick secondPick,
        double distance,
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
                errorMessage = "Second chamfer object must be different from the first one.";
                return false;
            }

            return TryCreateLineLineChamfer(
                firstLine,
                firstPick.PickPoint,
                secondLine,
                secondPick.PickPoint,
                distance,
                tolerance,
                out resultEntities,
                out errorMessage);
        }

        if (firstPick.Entity is PolylineEntity firstPolyline &&
            secondPick.Entity is PolylineEntity secondPolyline)
        {
            if (firstPick.EntityId.Equals(secondPick.EntityId))
            {
                return TryCreatePolylineSegmentChamfer(
                    firstPolyline,
                    firstPick.PolylineSegmentIndex,
                    secondPick.PolylineSegmentIndex,
                    distance,
                    tolerance,
                    out resultEntities,
                    out errorMessage);
            }

            return TryCreateSeparateObjectChamfer(
                firstPick,
                secondPick,
                distance,
                tolerance,
                out resultEntities,
                out errorMessage);
        }

        if ((firstPick.Entity is LineEntity && secondPick.Entity is PolylineEntity) ||
            (firstPick.Entity is PolylineEntity && secondPick.Entity is LineEntity))
        {
            return TryCreateSeparateObjectChamfer(
                firstPick,
                secondPick,
                distance,
                tolerance,
                out resultEntities,
                out errorMessage);
        }

        errorMessage = "Polyline segment chamfer currently supports lines, two adjacent segments of the same linear polyline, terminal segments of separate linear polylines, or line plus terminal polyline segment.";
        return false;
    }

    private static IReadOnlyList<CadEntity> GetRemovedEntitiesForChamfer(
        ChamferPick firstPick,
        ChamferPick secondPick)
    {
        if (firstPick.EntityId.Equals(secondPick.EntityId))
        {
            return new[] { firstPick.Entity };
        }

        return new[] { firstPick.Entity, secondPick.Entity };
    }

    internal static bool TryCreateLineLineChamfer(
        LineEntity first,
        Point2D firstPickPoint,
        LineEntity second,
        Point2D secondPickPoint,
        double distance,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (distance <= tolerance.Distance)
        {
            errorMessage = "Chamfer distance must be greater than zero.";
            return false;
        }

        if (!LineIntersectionService.TryIntersectInfiniteLines(
                first.Geometry,
                second.Geometry,
                out LineIntersectionInfo intersection,
                tolerance))
        {
            errorMessage = "Cannot chamfer parallel or coincident lines.";
            return false;
        }

        Vector2D firstDirection = first.Start.VectorTo(first.End);
        Vector2D secondDirection = second.Start.VectorTo(second.End);

        if (tolerance.IsVectorLengthZero(firstDirection.Length) ||
            tolerance.IsVectorLengthZero(secondDirection.Length))
        {
            errorMessage = "Cannot chamfer zero-length lines.";
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
        double minimumAngle = Math.Max(tolerance.Angle, 1e-6);

        if (angle <= minimumAngle || Math.Abs(Math.PI - angle) <= minimumAngle)
        {
            errorMessage = "Cannot chamfer lines with an invalid or nearly collinear corner angle.";
            return false;
        }

        Point2D firstChamferPoint = intersectionPoint + firstBranch * distance;
        Point2D secondChamferPoint = intersectionPoint + secondBranch * distance;

        resultEntities = new CadEntity[]
        {
            CreateTrimmedLineToPoint(first, firstBranch, firstChamferPoint),
            CreateTrimmedLineToPoint(second, secondBranch, secondChamferPoint),
            new LineEntity(
                firstChamferPoint,
                secondChamferPoint,
                layerId: first.LayerId,
                style: first.Style,
                isVisible: first.IsVisible,
                isLocked: first.IsLocked,
                drawOrder: Math.Max(first.DrawOrder, second.DrawOrder) + 1)
        };
        return true;
    }

    private static bool TryCreateSeparateObjectChamfer(
        ChamferPick firstPick,
        ChamferPick secondPick,
        double distance,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (!TryCreateChamferLineSource(firstPick, out LineEntity? firstLine, out ChamferSourceKind firstKind, out errorMessage) ||
            firstLine is null)
        {
            return false;
        }

        if (!TryCreateChamferLineSource(secondPick, out LineEntity? secondLine, out ChamferSourceKind secondKind, out errorMessage) ||
            secondLine is null)
        {
            return false;
        }

        if (!TryCreateLineLineChamfer(
                firstLine,
                firstPick.PickPoint,
                secondLine,
                secondPick.PickPoint,
                distance,
                tolerance,
                out IReadOnlyList<CadEntity>? lineResultEntities,
                out errorMessage))
        {
            return false;
        }

        if (lineResultEntities is null || lineResultEntities.Count != 3)
        {
            errorMessage ??= "Cannot create chamfer for the selected objects.";
            return false;
        }

        if (lineResultEntities[0] is not LineEntity firstTrimmed ||
            lineResultEntities[1] is not LineEntity secondTrimmed ||
            lineResultEntities[2] is not LineEntity chamferLine)
        {
            errorMessage = "Cannot create chamfer geometry for the selected objects.";
            return false;
        }

        if (!TryConvertTrimmedChamferSource(firstTrimmed, firstPick, firstKind, tolerance, out CadEntity? convertedFirst, out errorMessage) ||
            convertedFirst is null)
        {
            return false;
        }

        if (!TryConvertTrimmedChamferSource(secondTrimmed, secondPick, secondKind, tolerance, out CadEntity? convertedSecond, out errorMessage) ||
            convertedSecond is null)
        {
            return false;
        }

        resultEntities = new CadEntity[]
        {
            convertedFirst,
            convertedSecond,
            chamferLine
        };

        return true;
    }

    private static bool TryCreateChamferLineSource(
        ChamferPick pick,
        out LineEntity? line,
        out ChamferSourceKind kind,
        out string? errorMessage)
    {
        line = null;
        kind = ChamferSourceKind.Line;
        errorMessage = null;

        if (pick.Entity is LineEntity lineEntity)
        {
            line = lineEntity;
            kind = ChamferSourceKind.Line;
            return true;
        }

        if (pick.Entity is not PolylineEntity polyline)
        {
            errorMessage = "Chamfer currently supports visible, unlocked lines and polyline segments only.";
            return false;
        }

        kind = ChamferSourceKind.PolylineSegment;

        if (pick.PolylineSegmentIndex is null)
        {
            errorMessage = "Select a polyline segment.";
            return false;
        }

        if (polyline.IsClosed)
        {
            errorMessage = "Chamfer between separate objects currently supports open polylines only.";
            return false;
        }

        if (polyline.HasArcSegments)
        {
            errorMessage = "Polyline segment chamfer currently supports linear polylines only.";
            return false;
        }

        if (!IsTerminalOpenPolylineSegment(polyline, pick.PolylineSegmentIndex.Value))
        {
            errorMessage = "Chamfer between separate polylines currently supports terminal segments only.";
            return false;
        }

        line = CreateLineFromPolylineSegment(polyline, pick.PolylineSegmentIndex.Value);
        return true;
    }

    private static bool TryConvertTrimmedChamferSource(
        LineEntity trimmedLine,
        ChamferPick originalPick,
        ChamferSourceKind kind,
        GeometryTolerance tolerance,
        out CadEntity? converted,
        out string? errorMessage)
    {
        converted = null;
        errorMessage = null;

        if (kind == ChamferSourceKind.Line)
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
            errorMessage = "Chamfer between separate polylines currently supports terminal segments only.";
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
                errorMessage = "Chamfer between separate polylines can only trim the terminal endpoint of a multi-segment polyline.";
                return false;
            }

            vertices[0] = trimmedLine.Start;
        }
        else if (segmentIndex == sourcePolyline.SegmentCount - 1)
        {
            if (startChanged)
            {
                errorMessage = "Chamfer between separate polylines can only trim the terminal endpoint of a multi-segment polyline.";
                return false;
            }

            vertices[^1] = trimmedLine.End;
        }
        else
        {
            errorMessage = "Chamfer between separate polylines currently supports terminal segments only.";
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

    private static bool TryCreatePolylineSegmentChamfer(
        PolylineEntity polyline,
        int? firstSegmentIndex,
        int? secondSegmentIndex,
        double distance,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (distance <= tolerance.Distance)
        {
            errorMessage = "Polyline segment chamfer requires a distance greater than zero.";
            return false;
        }

        if (firstSegmentIndex is null || secondSegmentIndex is null)
        {
            errorMessage = "Select two polyline segments.";
            return false;
        }

        if (polyline.HasArcSegments)
        {
            errorMessage = "Polyline segment chamfer currently supports linear polylines only.";
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
            errorMessage = "Cannot chamfer zero-length polyline segments.";
            return false;
        }

        if (distance >= common.DistanceTo(previous) - tolerance.Distance ||
            distance >= common.DistanceTo(next) - tolerance.Distance)
        {
            errorMessage = "Chamfer distance is too large for the selected polyline segments.";
            return false;
        }

        Vector2D previousUnit = previousBranch.Normalize();
        Vector2D nextUnit = nextBranch.Normalize();
        double angle = Math.Acos(Math.Clamp(previousUnit.Dot(nextUnit), -1.0, 1.0));
        double minimumAngle = Math.Max(tolerance.Angle, 1e-6);

        if (angle <= minimumAngle || Math.Abs(Math.PI - angle) <= minimumAngle)
        {
            errorMessage = "Cannot chamfer polyline segments with an invalid or nearly collinear corner angle.";
            return false;
        }

        Point2D previousChamferPoint = common + previousUnit * distance;
        Point2D nextChamferPoint = common + nextUnit * distance;

        IReadOnlyList<PolylineSegmentPiece> pieces = BuildChamferedPolylineSegments(
            polyline,
            beforeSegmentIndex,
            afterSegmentIndex,
            previousChamferPoint,
            nextChamferPoint,
            tolerance);

        if (pieces.Count == 0)
        {
            errorMessage = "Cannot create chamfered polyline geometry.";
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

        resultEntities = new CadEntity[]
        {
            new PolylineEntity(
                vertices,
                polyline.IsClosed,
                layerId: polyline.LayerId,
                style: polyline.Style,
                isVisible: polyline.IsVisible,
                isLocked: polyline.IsLocked,
                drawOrder: polyline.DrawOrder,
                isFilled: polyline.IsClosed && polyline.IsFilled,
                segmentBulges: bulges)
        };

        return true;
    }

    private static IReadOnlyList<PolylineSegmentPiece> BuildChamferedPolylineSegments(
        PolylineEntity polyline,
        int beforeSegmentIndex,
        int afterSegmentIndex,
        Point2D previousChamferPoint,
        Point2D nextChamferPoint,
        GeometryTolerance tolerance)
    {
        var pieces = new List<PolylineSegmentPiece>();

        for (int index = 0; index < polyline.SegmentCount; index++)
        {
            Point2D start = polyline.Vertices[index];
            Point2D end = polyline.Vertices[(index + 1) % polyline.Vertices.Count];

            if (index == beforeSegmentIndex)
            {
                AddPolylinePieceIfLongEnough(pieces, start, previousChamferPoint, 0.0, tolerance);

                if (afterSegmentIndex == (beforeSegmentIndex + 1) % polyline.SegmentCount)
                {
                    AddPolylinePieceIfLongEnough(pieces, previousChamferPoint, nextChamferPoint, 0.0, tolerance);
                }

                continue;
            }

            if (index == afterSegmentIndex)
            {
                AddPolylinePieceIfLongEnough(pieces, nextChamferPoint, end, 0.0, tolerance);
                continue;
            }

            AddPolylinePieceIfLongEnough(pieces, start, end, 0.0, tolerance);
        }

        if (polyline.IsClosed && afterSegmentIndex == 0 && beforeSegmentIndex == polyline.SegmentCount - 1)
        {
            AddPolylinePieceIfLongEnough(pieces, previousChamferPoint, nextChamferPoint, 0.0, tolerance);
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

    private static double GetLineParameter(
        LineEntity line,
        Point2D point)
    {
        Vector2D direction = line.Start.VectorTo(line.End);
        double lengthSquared = direction.LengthSquared;
        if (lengthSquared <= 0)
        {
            return 0;
        }

        return line.Start.VectorTo(point).Dot(direction) / lengthSquared;
    }

    private static ChamferPick? PickSelectableChamferObject(
        ToolContext context,
        Point2D pickPoint,
        ChamferPick? firstPick = null)
    {
        IReadOnlyList<EntityId> selectedIds = context.Selection.Service.SelectAllByPoint(
            context.Document,
            pickPoint,
            context.Selection.Tolerance);

        ChamferPick? pick = PickChamferObjectFromEntities(
            context,
            selectedIds.Select(id => context.Document.Entities.GetRequired(id)),
            pickPoint,
            firstPick,
            requireDistanceWithinTolerance: false);

        if (pick is not null)
        {
            return pick;
        }

        return PickChamferObjectFromEntities(
            context,
            context.Document.GetSelectableEntities(),
            pickPoint,
            firstPick,
            requireDistanceWithinTolerance: true);
    }

    private static ChamferPick? PickChamferObjectFromEntities(
        ToolContext context,
        IEnumerable<CadEntity> entities,
        Point2D pickPoint,
        ChamferPick? firstPick,
        bool requireDistanceWithinTolerance)
    {
        ChamferPick? bestPick = null;
        double bestDistance = double.PositiveInfinity;

        foreach (CadEntity entity in entities)
        {
            if (!context.Document.IsEntitySelectable(entity))
            {
                continue;
            }

            ChamferPick? candidate = TryCreateChamferPick(
                entity,
                pickPoint,
                firstPick);

            if (candidate is null)
            {
                continue;
            }

            double distance = candidate.ClosestPoint.DistanceTo(pickPoint);
            if (requireDistanceWithinTolerance && distance > context.Selection.Tolerance)
            {
                continue;
            }

            if (distance < bestDistance ||
                (Tolerance.AreEqual(distance, bestDistance) && entity.DrawOrder > (bestPick?.Entity.DrawOrder ?? int.MinValue)))
            {
                bestPick = candidate;
                bestDistance = distance;
            }
        }

        return bestPick;
    }

    private static ChamferPick? TryCreateChamferPick(
        CadEntity entity,
        Point2D pickPoint,
        ChamferPick? firstPick)
    {
        if (entity is LineEntity)
        {
            if (firstPick is not null && entity.Id.Equals(firstPick.EntityId))
            {
                return null;
            }

            return new ChamferPick(
                entity.Id,
                pickPoint,
                entity.GetClosestPoint(pickPoint),
                entity,
                null);
        }

        if (entity is PolylineEntity polyline)
        {
            int? excludedSegmentIndex = firstPick is not null && entity.Id.Equals(firstPick.EntityId)
                ? firstPick.PolylineSegmentIndex
                : null;

            int? segmentIndex = FindClosestLinearPolylineSegment(
                polyline,
                pickPoint,
                excludedSegmentIndex);

            if (segmentIndex is null)
            {
                return null;
            }

            return new ChamferPick(
                entity.Id,
                pickPoint,
                GetClosestPointOnSegment(
                    polyline.Vertices[segmentIndex.Value],
                    polyline.Vertices[(segmentIndex.Value + 1) % polyline.Vertices.Count],
                    pickPoint),
                entity,
                segmentIndex.Value);
        }

        return null;
    }

    private static int? FindClosestLinearPolylineSegment(
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

            if (index < polyline.SegmentBulges.Count && !Tolerance.IsZero(polyline.SegmentBulges[index]))
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
        Vector2D direction = start.VectorTo(end);
        double lengthSquared = direction.LengthSquared;

        if (Tolerance.IsZero(lengthSquared))
        {
            return start;
        }

        double parameter = Math.Clamp(start.VectorTo(point).Dot(direction) / lengthSquared, 0.0, 1.0);
        return start + direction * parameter;
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

    private sealed record ChamferPick(
        EntityId EntityId,
        Point2D PickPoint,
        Point2D ClosestPoint,
        CadEntity Entity,
        int? PolylineSegmentIndex);

    private readonly record struct PolylineSegmentPiece(
        Point2D Start,
        Point2D End,
        double Bulge);

    private enum ChamferSourceKind
    {
        Line,
        PolylineSegment
    }

    private void Reset(ToolContext context)
    {
        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = ChamferToolState.WaitingForFirstEntityOrDistance;
        context.CurrentBasePoint = null;
    }
}
