using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates parallel/constant-distance copies of supported entities.
/// Supports lines, circles, arcs and straight-segment polylines with miter joins.
/// </summary>
public sealed class OffsetTool : ICadTool, ICommandDrivenTool
{
    private double? _distance;
    private ToolPickedEntityInput? _pickedEntity;
    private CadEntity? _previewEntity;
    private Point2D? _currentSidePoint;

    public string Name => "Offset";

    public OffsetToolState State { get; private set; } = OffsetToolState.WaitingForDistance;

    public double? Distance => _distance;

    public Point2D? CurrentSidePoint => _currentSidePoint;

    public CadEntity? GetPreviewEntity()
    {
        return _previewEntity;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            OffsetToolState.WaitingForDistance => new CommandPromptState(
                "OFFSET",
                "Specify offset distance",
                CommandInputKind.Distance,
                placeholder: "Distance, for example 100"),

            OffsetToolState.WaitingForEntity => new CommandPromptState(
                "OFFSET",
                "Select object to offset",
                CommandInputKind.Selection,
                placeholder: "Click line, circle, arc or polyline"),

            OffsetToolState.WaitingForSidePoint => new CommandPromptState(
                "OFFSET",
                "Specify side to offset",
                CommandInputKind.Point,
                placeholder: "Click side or type a point"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == OffsetToolState.WaitingForDistance &&
            input.Kind == CommandInputSubmissionKind.Distance &&
            input.Distance is not null)
        {
            return AcceptDistance(context, input.Distance.Value);
        }

        if (State == OffsetToolState.WaitingForSidePoint &&
            input.Kind == CommandInputSubmissionKind.Point &&
            input.Point is not null)
        {
            return CreateOffset(context, input.Point.Value);
        }

        return State switch
        {
            OffsetToolState.WaitingForDistance => ToolResult.None("Specify a positive offset distance."),
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
            OffsetToolState.WaitingForDistance => ToolResult.None("Type the offset distance in the command input."),
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
            ? ToolResult.Updated("Offset preview updated.")
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

    private ToolResult AcceptDistance(
        ToolContext context,
        double distance)
    {
        if (distance <= 0 || context.GeometryTolerance.IsDistanceZero(distance))
        {
            return ToolResult.None("Offset distance must be greater than zero.");
        }

        _distance = distance;
        _pickedEntity = null;
        State = OffsetToolState.WaitingForEntity;
        context.CurrentBasePoint = null;

        return ToolResult.Started("Select object to offset.");
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
            return ToolResult.None("Offset currently supports lines, circles, arcs and straight-segment polylines.");
        }

        _pickedEntity = picked;
        State = OffsetToolState.WaitingForSidePoint;
        context.CurrentBasePoint = picked.ClosestPoint;

        return ToolResult.Started("Specify side to offset.");
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

        if (!TryCreateOffsetEntity(
                _pickedEntity.Entity,
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
                errorMessage = "Offset currently supports lines, circles, arcs and straight-segment polylines.";
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

        IReadOnlyList<Point2D> vertices = polyline.Vertices;
        if (vertices.Count < 2)
        {
            errorMessage = "Cannot offset a polyline with fewer than two vertices.";
            return false;
        }

        IReadOnlyList<LineSegment2D> originalSegments = polyline.Geometry.GetSegments();
        if (originalSegments.Count == 0)
        {
            errorMessage = "Cannot offset a polyline without segments.";
            return false;
        }

        int sideSign = DeterminePolylineOffsetSide(originalSegments, sidePoint, tolerance);
        if (sideSign == 0)
        {
            sideSign = 1;
        }

        var offsetSegments = new List<LineSegment2D>(originalSegments.Count);
        foreach (LineSegment2D segment in originalSegments)
        {
            Vector2D direction = segment.Start.VectorTo(segment.End);
            if (tolerance.IsVectorLengthZero(direction.Length))
            {
                errorMessage = "Cannot offset a polyline containing zero-length segments.";
                return false;
            }

            Vector2D normal = direction.Normalize().PerpendicularLeft() * sideSign;
            Vector2D offset = normal * distance;
            offsetSegments.Add(new LineSegment2D(
                segment.Start + offset,
                segment.End + offset));
        }

        var offsetVertices = polyline.IsClosed
            ? BuildClosedOffsetVertices(offsetSegments, tolerance)
            : BuildOpenOffsetVertices(offsetSegments, tolerance);

        if (offsetVertices.Count < 2)
        {
            errorMessage = "Cannot offset the selected polyline.";
            return false;
        }

        if (HasDuplicateConsecutiveVertices(offsetVertices, polyline.IsClosed, tolerance))
        {
            errorMessage = "Polyline offset failed because the resulting geometry contains degenerate segments.";
            return false;
        }

        offsetPolyline = new PolylineEntity(
            offsetVertices,
            polyline.IsClosed,
            layerId: polyline.LayerId,
            style: polyline.Style,
            isVisible: polyline.IsVisible,
            isLocked: polyline.IsLocked,
            drawOrder: polyline.DrawOrder + 1);
        return true;
    }

    private static int DeterminePolylineOffsetSide(
        IReadOnlyList<LineSegment2D> segments,
        Point2D sidePoint,
        GeometryTolerance tolerance)
    {
        LineSegment2D closestSegment = segments[0];
        double bestDistance = double.MaxValue;

        foreach (LineSegment2D segment in segments)
        {
            Point2D closest = ClosestPointOnSegment(segment, sidePoint, tolerance);
            double distance = closest.DistanceTo(sidePoint);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closestSegment = segment;
            }
        }

        Vector2D direction = closestSegment.Start.VectorTo(closestSegment.End);
        double cross = direction.Cross(closestSegment.Start.VectorTo(sidePoint));
        if (tolerance.IsDistanceZero(cross))
        {
            return 1;
        }

        return cross >= 0 ? 1 : -1;
    }

    private static List<Point2D> BuildOpenOffsetVertices(
        IReadOnlyList<LineSegment2D> offsetSegments,
        GeometryTolerance tolerance)
    {
        var vertices = new List<Point2D>
        {
            offsetSegments[0].Start
        };

        for (int index = 1; index < offsetSegments.Count; index++)
        {
            vertices.Add(IntersectOffsetSegments(
                offsetSegments[index - 1],
                offsetSegments[index],
                tolerance));
        }

        vertices.Add(offsetSegments[^1].End);
        return vertices;
    }

    private static List<Point2D> BuildClosedOffsetVertices(
        IReadOnlyList<LineSegment2D> offsetSegments,
        GeometryTolerance tolerance)
    {
        var vertices = new List<Point2D>(offsetSegments.Count);

        for (int index = 0; index < offsetSegments.Count; index++)
        {
            LineSegment2D previous = offsetSegments[(index - 1 + offsetSegments.Count) % offsetSegments.Count];
            LineSegment2D current = offsetSegments[index];
            vertices.Add(IntersectOffsetSegments(previous, current, tolerance));
        }

        return vertices;
    }

    private static Point2D IntersectOffsetSegments(
        LineSegment2D previous,
        LineSegment2D current,
        GeometryTolerance tolerance)
    {
        if (TryIntersectInfiniteLines(previous, current, tolerance, out Point2D intersection))
        {
            return intersection;
        }

        if (tolerance.ArePointsEqual(previous.End, current.Start))
        {
            return previous.End;
        }

        return new Point2D(
            (previous.End.X + current.Start.X) / 2.0,
            (previous.End.Y + current.Start.Y) / 2.0);
    }

    private static bool TryIntersectInfiniteLines(
        LineSegment2D first,
        LineSegment2D second,
        GeometryTolerance tolerance,
        out Point2D intersection)
    {
        intersection = Point2D.Origin;

        Point2D p = first.Start;
        Point2D q = second.Start;
        Vector2D r = first.Start.VectorTo(first.End);
        Vector2D s = second.Start.VectorTo(second.End);
        double cross = r.Cross(s);

        if (tolerance.IsVectorLengthZero(r.Length) ||
            tolerance.IsVectorLengthZero(s.Length) ||
            tolerance.IsDistanceZero(cross))
        {
            return false;
        }

        Vector2D qMinusP = p.VectorTo(q);
        double t = qMinusP.Cross(s) / cross;
        intersection = new Point2D(
            p.X + (t * r.X),
            p.Y + (t * r.Y));
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

    private void UpdatePreview(
        ToolContext context,
        Point2D sidePoint)
    {
        _currentSidePoint = sidePoint;

        if (_distance is null || _pickedEntity is null)
        {
            _previewEntity = null;
            return;
        }

        if (TryCreateOffsetEntity(
                _pickedEntity.Entity,
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

    private void Reset(ToolContext context)
    {
        _distance = null;
        _pickedEntity = null;
        _previewEntity = null;
        _currentSidePoint = null;
        State = OffsetToolState.WaitingForDistance;
        context.CurrentBasePoint = null;
    }
}
