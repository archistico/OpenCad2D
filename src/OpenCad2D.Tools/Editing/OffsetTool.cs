using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates parallel/constant-distance copies of supported entities.
/// Supports lines, circles, arcs and straight-segment polylines with miter joins and approximate mixed-polylines.
/// True offsets of ellipses, elliptical arcs and splines are intentionally deferred.
/// </summary>
public sealed class OffsetTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, IToolPreviewDescriptorProvider, ISnapModeProvider
{
    private const double MiterLimitRatio = 10.0;

    private static double? s_lastDistance;

    private double? _distance;
    private Point2D? _distanceFirstPoint;
    private ToolPickedEntityInput? _pickedEntity;
    private CadEntity? _previewEntity;
    private Point2D? _currentSidePoint;

    public string Name => "Offset";

    public OffsetToolState State { get; private set; } = OffsetToolState.WaitingForDistance;

    public bool IsCommandFinished { get; private set; }

    public double? Distance => _distance;

    public double? LastDistance => s_lastDistance;

    public Point2D? DistanceFirstPoint => _distanceFirstPoint;

    public Point2D? CurrentSidePoint => _currentSidePoint;

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            OffsetToolState.WaitingForEntity => SnapKind.EntityOnly,
            OffsetToolState.WaitingForDistance or OffsetToolState.WaitingForDistanceSecondPoint => context.EnabledSnaps & ~SnapKind.Entity,
            OffsetToolState.WaitingForSidePoint => context.EnabledSnaps & ~SnapKind.Entity,
            _ => context.EnabledSnaps
        };
    }

    public static void ResetLastDistanceForTests()
    {
        s_lastDistance = null;
    }

    public CadEntity? GetPreviewEntity()
    {
        return _previewEntity;
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _previewEntity is null
            ? Array.Empty<CadEntity>()
            : new[] { _previewEntity };
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ToolPreviewDescriptor(
            highlightedEntities: GetPreviewEntities(context),
            highlightedEntityKind: ToolPreviewHighlightKind.Addition,
            entityOverlays: GetSelectedTargetOverlays());
    }

    private IReadOnlyList<ToolPreviewEntityOverlay> GetSelectedTargetOverlays()
    {
        if (_pickedEntity is null)
        {
            return Array.Empty<ToolPreviewEntityOverlay>();
        }

        return new[]
        {
            new ToolPreviewEntityOverlay(
                new[] { _pickedEntity.Entity },
                ToolPreviewHighlightKind.Emphasis)
        };
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            OffsetToolState.WaitingForDistance => new CommandPromptState(
                "OFFSET",
                s_lastDistance is null
                    ? "Specify offset distance or first distance point"
                    : $"Specify offset distance or first distance point <{FormatDistance(s_lastDistance.Value)}>",
                // The prompt offers both a scalar distance and a first measuring point.
                // PointOrNumber keeps typed scalars as plain numbers: PointOrDistance would
                // make the command line convert them into direct-distance points, which
                // needs a base point that does not exist in this phase.
                CommandInputKind.PointOrNumber,
                acceptsEmptyEnter: true,
                placeholder: s_lastDistance is null
                    ? "Distance, for example 100, or click first point"
                    : "Distance, first point, Enter or right-click for default"),

            OffsetToolState.WaitingForDistanceSecondPoint => new CommandPromptState(
                "OFFSET",
                "Specify second distance point or type distance",
                CommandInputKind.PointOrNumber,
                placeholder: "Click second point or type a distance"),

            OffsetToolState.WaitingForEntity => new CommandPromptState(
                "OFFSET",
                "Select object to offset or press Enter to finish",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Click line, circle, arc or polyline; Enter finishes"),

            OffsetToolState.WaitingForSidePoint => new CommandPromptState(
                "OFFSET",
                "Specify side to offset or press Enter to finish",
                CommandInputKind.Point,
                acceptsEmptyEnter: true,
                placeholder: "Click side or type a point; Enter finishes"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == OffsetToolState.WaitingForDistance)
        {
            if (TryGetTypedDistance(input, out double typedDistance))
            {
                return AcceptDistance(context, typedDistance, "Select object to offset.");
            }

            if (input.Kind == CommandInputSubmissionKind.Point && input.Point is not null)
            {
                return AcceptFirstDistancePoint(context, input.Point.Value);
            }

            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return ConfirmLastDistance(context);
            }
        }

        if (State == OffsetToolState.WaitingForDistanceSecondPoint)
        {
            if (TryGetTypedDistance(input, out double typedDistance))
            {
                return AcceptDistance(context, typedDistance, "Select object to offset.");
            }

            if (input.Kind == CommandInputSubmissionKind.Point && input.Point is not null)
            {
                return AcceptMeasuredDistance(context, input.Point.Value);
            }
        }

        if (State == OffsetToolState.WaitingForSidePoint &&
            input.Kind == CommandInputSubmissionKind.Point &&
            input.Point is not null)
        {
            return CreateOffset(context, input.Point.Value);
        }

        if ((State is OffsetToolState.WaitingForEntity or OffsetToolState.WaitingForSidePoint) &&
            input.Kind == CommandInputSubmissionKind.Confirm)
        {
            Reset(context);
            IsCommandFinished = true;
            return ToolResult.Completed("Offset command finished.");
        }

        return State switch
        {
            OffsetToolState.WaitingForDistance => ToolResult.None("Specify a positive offset distance, two distance points, or confirm the previous distance."),
            OffsetToolState.WaitingForDistanceSecondPoint => ToolResult.None("Specify the second distance point or type a positive offset distance."),
            OffsetToolState.WaitingForEntity => ToolResult.None("Select a line, circle, arc or polyline from the drawing canvas."),
            OffsetToolState.WaitingForSidePoint => ToolResult.None("Specify the side to offset by clicking or typing a point."),
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
            OffsetToolState.WaitingForDistance => AcceptFirstDistancePoint(context, pointer.ModelPoint),
            OffsetToolState.WaitingForDistanceSecondPoint => AcceptMeasuredDistance(context, pointer.ModelPoint),
            OffsetToolState.WaitingForEntity => AcceptEntity(context, pointer.ModelPoint),
            OffsetToolState.WaitingForSidePoint => CreateOffset(context, pointer.ModelPoint),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != OffsetToolState.WaitingForSidePoint)
        {
            return ToolResult.None();
        }

        UpdatePreview(
            context,
            pointer.ModelPoint);

        return _previewEntity is not null
            ? ToolResult.Updated("Offset preview updated. Highlighted preview shows the entity that will be created.")
            : ToolResult.None("Cannot preview offset on the selected side.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.Cancelled("Offset command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.None("Offset tool deactivated.");
    }

    private static bool TryGetTypedDistance(
        CommandInputSubmission input,
        out double distance)
    {
        if (input.Kind == CommandInputSubmissionKind.Distance && input.Distance is not null)
        {
            distance = input.Distance.Value;
            return true;
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            distance = input.Number.Value;
            return true;
        }

        distance = 0;
        return false;
    }

    private ToolResult AcceptFirstDistancePoint(
        ToolContext context,
        Point2D point)
    {
        _distanceFirstPoint = point;
        context.CurrentBasePoint = point;
        State = OffsetToolState.WaitingForDistanceSecondPoint;

        return ToolResult.Started("Specify second distance point or type distance.");
    }

    private ToolResult AcceptMeasuredDistance(
        ToolContext context,
        Point2D secondPoint)
    {
        if (_distanceFirstPoint is null)
        {
            State = OffsetToolState.WaitingForDistance;
            return ToolResult.None("Specify first distance point first.");
        }

        double measuredDistance = _distanceFirstPoint.Value.DistanceTo(secondPoint);
        return AcceptDistance(context, measuredDistance, $"Offset distance set to {FormatDistance(measuredDistance)}. Select object to offset.");
    }

    private ToolResult ConfirmLastDistance(ToolContext context)
    {
        if (s_lastDistance is null)
        {
            return ToolResult.None("No previous offset distance. Specify a distance first.");
        }

        return AcceptDistance(context, s_lastDistance.Value, $"Offset distance remains {FormatDistance(s_lastDistance.Value)}. Select object to offset.");
    }

    private ToolResult AcceptDistance(
        ToolContext context,
        double distance,
        string successMessage)
    {
        if (distance <= 0 || context.GeometryTolerance.IsDistanceZero(distance))
        {
            return ToolResult.None("Offset distance must be greater than zero.");
        }

        _distance = distance;
        s_lastDistance = distance;
        _distanceFirstPoint = null;
        _pickedEntity = null;
        State = OffsetToolState.WaitingForEntity;
        context.CurrentBasePoint = null;

        return ToolResult.Started(successMessage);
    }

    private ToolResult AcceptEntity(
        ToolContext context,
        Point2D pickPoint)
    {
        ToolPickedEntityInput? picked = PickSelectableEntity(context, pickPoint);

        if (picked is null)
        {
            return ToolResult.None("Select a visible, unlocked line, circle, arc or polyline to offset.");
        }

        if (!IsSupportedEntity(picked.Entity))
        {
            return ToolResult.None(GetUnsupportedEntityMessage(picked.Entity));
        }

        _pickedEntity = picked;
        State = OffsetToolState.WaitingForSidePoint;
        context.CurrentBasePoint = picked.ClosestPoint;

        return ToolResult.Started("Offset target selected. Specify side to offset.");
    }

    private ToolResult CreateOffset(
        ToolContext context,
        Point2D sidePoint)
    {
        if (_distance is null)
        {
            State = OffsetToolState.WaitingForDistance;
            return ToolResult.None("Specify offset distance first.");
        }

        if (_pickedEntity is null)
        {
            State = OffsetToolState.WaitingForEntity;
            return ToolResult.None("Select object to offset first.");
        }

        CadEntity? sourceEntity = ResolvePickedEntity(context);
        if (sourceEntity is null)
        {
            _pickedEntity = null;
            _previewEntity = null;
            _currentSidePoint = null;
            State = OffsetToolState.WaitingForEntity;
            context.CurrentBasePoint = null;
            return ToolResult.None("The selected object is no longer available. Select object to offset.");
        }

        if (!TryCreateOffsetEntity(
                sourceEntity,
                sidePoint,
                _distance.Value,
                context.GeometryTolerance,
                out CadEntity? offsetEntity,
                out string? errorMessage))
        {
            return ToolResult.None(errorMessage ?? "Cannot offset selected entity.");
        }

        if (offsetEntity is null)
        {
            return ToolResult.None(errorMessage ?? "Cannot offset selected entity.");
        }

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(offsetEntity));

        _pickedEntity = null;
        _previewEntity = null;
        _currentSidePoint = null;
        State = OffsetToolState.WaitingForEntity;
        context.CurrentBasePoint = null;

        return ToolResult.Completed("Offset entity created. Select another object to offset or press Escape.");
    }

    internal static bool TryCreateOffsetEntity(
        CadEntity entity,
        Point2D sidePoint,
        double distance,
        GeometryTolerance tolerance,
        out CadEntity? offsetEntity,
        out string? errorMessage)
    {
        offsetEntity = null;
        errorMessage = null;

        switch (entity)
        {
            case LineEntity line:
                Vector2D direction = line.Start.VectorTo(line.End);
                if (tolerance.IsVectorLengthZero(direction.Length))
                {
                    errorMessage = "Cannot offset a zero-length line.";
                    return false;
                }

                Vector2D unit = direction.Normalize();
                Vector2D left = unit.PerpendicularLeft();
                double side = direction.Cross(line.Start.VectorTo(sidePoint));
                Vector2D normal = side >= 0 ? left : left * -1.0;
                Vector2D offset = normal * distance;

                offsetEntity = new LineEntity(
                    line.Start + offset,
                    line.End + offset,
                    layerId: line.LayerId,
                    style: line.Style,
                    isVisible: line.IsVisible,
                    isLocked: line.IsLocked,
                    drawOrder: line.DrawOrder + 1);
                return true;

            case CircleEntity circle:
                double circleRadius = sidePoint.DistanceTo(circle.Center) >= circle.Radius
                    ? circle.Radius + distance
                    : circle.Radius - distance;

                if (circleRadius <= tolerance.Distance)
                {
                    errorMessage = "Offset distance would make the circle radius zero or negative.";
                    return false;
                }

                offsetEntity = new CircleEntity(
                    circle.Center,
                    circleRadius,
                    layerId: circle.LayerId,
                    style: circle.Style,
                    isVisible: circle.IsVisible,
                    isLocked: circle.IsLocked,
                    drawOrder: circle.DrawOrder + 1);
                return true;

            case ArcEntity arc:
                double arcRadius = sidePoint.DistanceTo(arc.Center) >= arc.Radius
                    ? arc.Radius + distance
                    : arc.Radius - distance;

                if (arcRadius <= tolerance.Distance)
                {
                    errorMessage = "Offset distance would make the arc radius zero or negative.";
                    return false;
                }

                offsetEntity = new ArcEntity(
                    arc.Center,
                    arcRadius,
                    arc.StartAngle,
                    arc.EndAngle,
                    arc.IsCounterClockwise,
                    layerId: arc.LayerId,
                    style: arc.Style,
                    isVisible: arc.IsVisible,
                    isLocked: arc.IsLocked,
                    drawOrder: arc.DrawOrder + 1);
                return true;


            case PolylineEntity polyline:
                if (!TryCreateOffsetPolyline(
                        polyline,
                        sidePoint,
                        distance,
                        tolerance,
                        out PolylineEntity? offsetPolyline,
                        out errorMessage))
                {
                    return false;
                }

                offsetEntity = offsetPolyline;
                return true;

            default:
                errorMessage = GetUnsupportedEntityMessage(entity);
                return false;
        }
    }

    internal static bool TryCreateOffsetPolyline(
        PolylineEntity polyline,
        Point2D sidePoint,
        double distance,
        GeometryTolerance tolerance,
        out PolylineEntity? offsetPolyline,
        out string? errorMessage)
    {
        offsetPolyline = null;
        errorMessage = null;

        if (polyline.Vertices.Count < 2)
        {
            errorMessage = "Cannot offset a polyline with fewer than two vertices.";
            return false;
        }

        bool isClosed = polyline.IsClosed;
        PolylineEntity sourcePolyline = polyline.HasArcSegments
            ? polyline.ToPolylineApproximation()
            : polyline;

        // Repeated consecutive vertices only produce zero-length segments without a direction.
        // They are common in imported closed polylines (closing vertex repeated at the end)
        // and in closed arc approximations, so they are skipped instead of rejected.
        List<Point2D> sourceVertices = RemoveDuplicateConsecutiveVertices(
            sourcePolyline.Vertices,
            isClosed,
            tolerance);

        if (sourceVertices.Count < 2)
        {
            errorMessage = "Cannot offset a polyline containing only zero-length segments.";
            return false;
        }

        if (isClosed && sourceVertices.Count < 3)
        {
            errorMessage = "Cannot offset a closed polyline with fewer than three distinct vertices.";
            return false;
        }

        List<LineSegment2D> sourceSegments = BuildSegments(sourceVertices, isClosed);

        int sideSign = DeterminePolylineOffsetSide(sourceSegments, sidePoint, tolerance);
        if (sideSign == 0)
        {
            sideSign = 1;
        }

        var directions = new List<Vector2D>(sourceSegments.Count);
        var offsetSegments = new List<LineSegment2D>(sourceSegments.Count);
        foreach (LineSegment2D segment in sourceSegments)
        {
            Vector2D direction = segment.Start.VectorTo(segment.End).Normalize();
            Vector2D offset = direction.PerpendicularLeft() * (sideSign * distance);
            directions.Add(direction);
            offsetSegments.Add(new LineSegment2D(
                segment.Start + offset,
                segment.End + offset));
        }

        // On the inner side of a corner the neighbouring offset segments trim each other.
        // When the distance is larger than a short segment allows, that segment is fully
        // consumed (its trimmed direction flips). Such segments are removed one at a time
        // and their neighbours are joined again, so tight corners and arc approximations
        // whose radius is smaller than the distance do not leave self-crossing loops.
        var activeSegments = Enumerable.Range(0, sourceSegments.Count).ToList();
        int minimumSegmentCount = isClosed ? 3 : 1;
        List<Point2D> offsetVertices;

        while (true)
        {
            offsetVertices = BuildOffsetVertices(
                activeSegments,
                sourceSegments,
                offsetSegments,
                directions,
                isClosed,
                distance,
                tolerance,
                out int collapsedPosition);

            if (collapsedPosition < 0)
            {
                break;
            }

            activeSegments.RemoveAt(collapsedPosition);
            if (activeSegments.Count < minimumSegmentCount)
            {
                errorMessage = "Offset distance is too large for the selected polyline on this side.";
                return false;
            }
        }

        if (isClosed &&
            offsetVertices.Count > 1 &&
            tolerance.ArePointsEqual(offsetVertices[0], offsetVertices[^1]))
        {
            offsetVertices.RemoveAt(offsetVertices.Count - 1);
        }

        if (offsetVertices.Count < (isClosed ? 3 : 2))
        {
            errorMessage = "Cannot offset the selected polyline.";
            return false;
        }

        if (HasDuplicateConsecutiveVertices(offsetVertices, isClosed, tolerance))
        {
            errorMessage = "Polyline offset failed because the resulting geometry contains degenerate segments.";
            return false;
        }

        offsetPolyline = new PolylineEntity(
            offsetVertices,
            isClosed,
            layerId: polyline.LayerId,
            style: polyline.Style,
            isVisible: polyline.IsVisible,
            isLocked: polyline.IsLocked,
            drawOrder: polyline.DrawOrder + 1);
        return true;
    }

    private static List<Point2D> RemoveDuplicateConsecutiveVertices(
        IReadOnlyList<Point2D> vertices,
        bool isClosed,
        GeometryTolerance tolerance)
    {
        var result = new List<Point2D>(vertices.Count);
        foreach (Point2D vertex in vertices)
        {
            AddVertexIfDistinct(result, vertex, tolerance);
        }

        while (isClosed &&
               result.Count > 1 &&
               tolerance.ArePointsEqual(result[0], result[^1]))
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    private static List<LineSegment2D> BuildSegments(
        IReadOnlyList<Point2D> vertices,
        bool isClosed)
    {
        int segmentCount = isClosed ? vertices.Count : vertices.Count - 1;
        var segments = new List<LineSegment2D>(segmentCount);
        for (int index = 0; index < segmentCount; index++)
        {
            segments.Add(new LineSegment2D(
                vertices[index],
                vertices[(index + 1) % vertices.Count]));
        }

        return segments;
    }

    private static int DeterminePolylineOffsetSide(
        IReadOnlyList<LineSegment2D> segments,
        Point2D sidePoint,
        GeometryTolerance tolerance)
    {
        double bestDistance = double.MaxValue;
        foreach (LineSegment2D segment in segments)
        {
            double distance = ClosestPointOnSegment(segment, sidePoint, tolerance).DistanceTo(sidePoint);
            bestDistance = Math.Min(bestDistance, distance);
        }

        // Several segments can be equally close: typically the side point lies on the
        // extension of one segment beyond a shared vertex (for example with grid or ortho
        // input). That segment cannot tell the side, while its neighbour can, so among the
        // closest segments the one that sees the point farthest from its own line wins.
        double bestSignedDistance = 0.0;
        foreach (LineSegment2D segment in segments)
        {
            Vector2D direction = segment.Start.VectorTo(segment.End);
            double length = direction.Length;
            if (tolerance.IsVectorLengthZero(length))
            {
                continue;
            }

            double distance = ClosestPointOnSegment(segment, sidePoint, tolerance).DistanceTo(sidePoint);
            if (distance > bestDistance + tolerance.Distance)
            {
                continue;
            }

            double signedDistance = direction.Cross(segment.Start.VectorTo(sidePoint)) / length;
            if (Math.Abs(signedDistance) > Math.Abs(bestSignedDistance))
            {
                bestSignedDistance = signedDistance;
            }
        }

        if (tolerance.IsDistanceZero(bestSignedDistance))
        {
            return 1;
        }

        return bestSignedDistance > 0 ? 1 : -1;
    }

    /// <summary>
    /// Builds the offset vertex chain for the active segments and reports the position of the
    /// most consumed segment, or -1 when every trimmed segment keeps its original direction.
    /// </summary>
    private static List<Point2D> BuildOffsetVertices(
        IReadOnlyList<int> activeSegments,
        IReadOnlyList<LineSegment2D> sourceSegments,
        IReadOnlyList<LineSegment2D> offsetSegments,
        IReadOnlyList<Vector2D> directions,
        bool isClosed,
        double distance,
        GeometryTolerance tolerance,
        out int collapsedPosition)
    {
        int count = activeSegments.Count;
        int firstJoin = isClosed ? 0 : 1;

        // Join at position k connects active segment k - 1 (its end) with active segment k (its start).
        var previousEnds = new Point2D[count];
        var currentStarts = new Point2D[count];

        for (int position = firstJoin; position < count; position++)
        {
            int previousIndex = activeSegments[(position - 1 + count) % count];
            int currentIndex = activeSegments[position];

            ComputeOffsetJoin(
                sourceSegments[previousIndex].End,
                offsetSegments[previousIndex],
                offsetSegments[currentIndex],
                distance,
                tolerance,
                out previousEnds[position],
                out currentStarts[position]);
        }

        // A trimmed segment is invalid when it is consumed (zero or reversed length) or when
        // it lies completely outside the extent of its own source segment: such a piece is
        // farther than the offset distance from every source point it could come from, which
        // happens to the leftover chords of an arc approximation whose radius is smaller than
        // the distance. Consumed segments are removed first, then the farthest stray piece.
        int consumedPosition = -1;
        double mostConsumedLength = double.MaxValue;
        int strayPosition = -1;
        double largestStrayGap = 0.0;

        for (int position = 0; position < count; position++)
        {
            int segmentIndex = activeSegments[position];
            LineSegment2D offsetSegment = offsetSegments[segmentIndex];
            Vector2D direction = directions[segmentIndex];

            Point2D start = !isClosed && position == 0
                ? offsetSegment.Start
                : currentStarts[position];

            Point2D end = !isClosed && position == count - 1
                ? offsetSegment.End
                : previousEnds[(position + 1) % count];

            double startParameter = offsetSegment.Start.VectorTo(start).Dot(direction);
            double endParameter = offsetSegment.Start.VectorTo(end).Dot(direction);
            double trimmedLength = endParameter - startParameter;

            if (trimmedLength <= tolerance.Distance)
            {
                if (trimmedLength < mostConsumedLength)
                {
                    mostConsumedLength = trimmedLength;
                    consumedPosition = position;
                }

                continue;
            }

            double segmentLength = offsetSegment.Start.DistanceTo(offsetSegment.End);
            double strayGap = Math.Max(
                -endParameter,
                startParameter - segmentLength);

            if (strayGap > tolerance.Distance &&
                strayGap > largestStrayGap)
            {
                largestStrayGap = strayGap;
                strayPosition = position;
            }
        }

        collapsedPosition = consumedPosition >= 0
            ? consumedPosition
            : strayPosition;

        var vertices = new List<Point2D>(count + 1);

        if (!isClosed)
        {
            AddVertexIfDistinct(vertices, offsetSegments[activeSegments[0]].Start, tolerance);
        }

        for (int position = firstJoin; position < count; position++)
        {
            AddVertexIfDistinct(vertices, previousEnds[position], tolerance);
            AddVertexIfDistinct(vertices, currentStarts[position], tolerance);
        }

        if (!isClosed)
        {
            AddVertexIfDistinct(vertices, offsetSegments[activeSegments[count - 1]].End, tolerance);
        }

        return vertices;
    }

    /// <summary>
    /// Joins two consecutive offset segments. Inner corners always use the exact
    /// intersection, because there the segments trim each other and a bevel would
    /// create a self-crossing loop. Outer corners use a miter up to the miter limit
    /// and a bevel beyond it. Parallel segments that do not meet are bevelled.
    /// </summary>
    private static void ComputeOffsetJoin(
        Point2D originalJoint,
        LineSegment2D previous,
        LineSegment2D current,
        double distance,
        GeometryTolerance tolerance,
        out Point2D previousEnd,
        out Point2D currentStart)
    {
        if (tolerance.ArePointsEqual(previous.End, current.Start))
        {
            previousEnd = previous.End;
            currentStart = previous.End;
            return;
        }

        if (TryIntersectInfiniteLines(
                previous,
                current,
                tolerance,
                out Point2D intersection,
                out double previousParameter,
                out double currentParameter))
        {
            bool isOuterJoin = previousParameter > 1.0 && currentParameter < 0.0;
            double maxMiterLength = Math.Max(distance * MiterLimitRatio, tolerance.Distance);

            if (!isOuterJoin ||
                originalJoint.DistanceTo(intersection) <= maxMiterLength)
            {
                previousEnd = intersection;
                currentStart = intersection;
                return;
            }
        }

        previousEnd = previous.End;
        currentStart = current.Start;
    }

    private static void AddVertexIfDistinct(
        List<Point2D> vertices,
        Point2D vertex,
        GeometryTolerance tolerance)
    {
        if (vertices.Count == 0 || !tolerance.ArePointsEqual(vertices[^1], vertex))
        {
            vertices.Add(vertex);
        }
    }

    private static bool TryIntersectInfiniteLines(
        LineSegment2D first,
        LineSegment2D second,
        GeometryTolerance tolerance,
        out Point2D intersection,
        out double firstParameter,
        out double secondParameter)
    {
        intersection = Point2D.Origin;
        firstParameter = 0.0;
        secondParameter = 0.0;

        Point2D p = first.Start;
        Point2D q = second.Start;
        Vector2D r = first.Start.VectorTo(first.End);
        Vector2D s = second.Start.VectorTo(second.End);
        double rLength = r.Length;
        double sLength = s.Length;

        if (tolerance.IsVectorLengthZero(rLength) ||
            tolerance.IsVectorLengthZero(sLength))
        {
            return false;
        }

        // Parallelism is tested on the normalized cross product (the sine of the angle),
        // so short segments such as arc approximation chords are not mistaken for parallel.
        double cross = r.Cross(s);
        if (Math.Abs(cross / (rLength * sLength)) <= tolerance.Angle)
        {
            return false;
        }

        Vector2D qMinusP = p.VectorTo(q);
        firstParameter = qMinusP.Cross(s) / cross;
        secondParameter = qMinusP.Cross(r) / cross;
        intersection = new Point2D(
            p.X + (firstParameter * r.X),
            p.Y + (firstParameter * r.Y));
        return true;
    }

    private static Point2D ClosestPointOnSegment(
        LineSegment2D segment,
        Point2D point,
        GeometryTolerance tolerance)
    {
        Vector2D direction = segment.Start.VectorTo(segment.End);
        double lengthSquared = direction.LengthSquared;
        if (tolerance.IsVectorLengthZero(Math.Sqrt(lengthSquared)))
        {
            return segment.Start;
        }

        double t = segment.Start.VectorTo(point).Dot(direction) / lengthSquared;
        t = Math.Clamp(t, 0.0, 1.0);

        return new Point2D(
            segment.Start.X + (direction.X * t),
            segment.Start.Y + (direction.Y * t));
    }

    private static bool HasDuplicateConsecutiveVertices(
        IReadOnlyList<Point2D> vertices,
        bool isClosed,
        GeometryTolerance tolerance)
    {
        for (int index = 0; index < vertices.Count - 1; index++)
        {
            if (tolerance.ArePointsEqual(vertices[index], vertices[index + 1]))
            {
                return true;
            }
        }

        return isClosed &&
               vertices.Count > 2 &&
               tolerance.ArePointsEqual(vertices[^1], vertices[0]);
    }

    private static ToolPickedEntityInput? PickSelectableEntity(
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

        return context.Document.IsEntitySelectable(entity)
            ? new ToolPickedEntityInput(
                selectedId.Value,
                pickPoint,
                entity.GetClosestPoint(pickPoint),
                entity)
            : null;
    }

    private static bool IsSupportedEntity(CadEntity entity)
    {
        return entity is LineEntity or CircleEntity or ArcEntity or PolylineEntity;
    }


    private static string GetUnsupportedEntityMessage(CadEntity entity)
    {
        return entity switch
        {
            EllipseEntity or EllipticalArcEntity =>
                "Offset currently supports lines, circles, arcs and polylines. Ellipse and elliptical arc offsets are deferred because a true offset is not another exact ellipse.",
            BezierSplineEntity =>
                "Offset currently supports lines, circles, arcs and polylines. Spline offsets are deferred because a true offset is not another exact Bezier spline.",
            _ =>
                "Offset currently supports lines, circles, arcs and polylines."
        };
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D sidePoint)
    {
        _currentSidePoint = sidePoint;

        CadEntity? sourceEntity = ResolvePickedEntity(context);
        if (_distance is null || sourceEntity is null)
        {
            _previewEntity = null;
            return;
        }

        if (TryCreateOffsetEntity(
                sourceEntity,
                sidePoint,
                _distance.Value,
                context.GeometryTolerance,
                out CadEntity? previewEntity,
                out _))
        {
            _previewEntity = previewEntity;
            return;
        }

        _previewEntity = null;
    }

    private static string FormatDistance(double value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Re-reads the picked target from the document so that an undo, delete or
    /// layer lock performed while choosing the side never offsets a stale copy.
    /// </summary>
    private CadEntity? ResolvePickedEntity(ToolContext context)
    {
        if (_pickedEntity is null ||
            !context.Document.Entities.TryGet(_pickedEntity.EntityId, out CadEntity? entity) ||
            entity is null ||
            !context.Document.IsEntitySelectable(entity) ||
            !IsSupportedEntity(entity))
        {
            return null;
        }

        return entity;
    }

    private void Reset(ToolContext context)
    {
        IsCommandFinished = false;
        _distance = null;
        _distanceFirstPoint = null;
        _pickedEntity = null;
        _previewEntity = null;
        _currentSidePoint = null;
        State = OffsetToolState.WaitingForDistance;
        context.CurrentBasePoint = null;
    }
}
