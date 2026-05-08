using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Operations;

/// <summary>
/// Provides intersection checks between rectangular boxes and geometric primitives.
/// </summary>
public static class RectangleIntersectionService
{
    public static bool IntersectsSegment(
        BoundingBox2D rectangle,
        LineSegment2D segment,
        double tolerance = Tolerance.Default)
    {
        if (rectangle.Contains(segment.Start) ||
            rectangle.Contains(segment.End))
        {
            return true;
        }

        foreach (LineSegment2D edge in rectangle.GetEdges())
        {
            IntersectionResult intersection =
                IntersectionService.IntersectSegments(
                    segment,
                    edge,
                    tolerance);

            if (intersection.HasIntersection)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IntersectsPolyline(
        BoundingBox2D rectangle,
        Polyline2D polyline,
        double tolerance = Tolerance.Default)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        foreach (Point2D vertex in polyline.Vertices)
        {
            if (rectangle.Contains(vertex))
            {
                return true;
            }
        }

        foreach (LineSegment2D segment in polyline.GetSegments())
        {
            if (IntersectsSegment(rectangle, segment, tolerance))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IntersectsCircle(
        BoundingBox2D rectangle,
        Circle2D circle,
        double tolerance = Tolerance.Default)
    {
        if (!rectangle.Intersects(circle.GetBoundingBox()))
        {
            return false;
        }

        double distance = DistanceService.DistancePointToBoundingBox(
            circle.Center,
            rectangle);

        return distance <= circle.Radius + tolerance;
    }

    public static bool IntersectsArc(
        BoundingBox2D rectangle,
        Arc2D arc,
        double tolerance = Tolerance.Default)
    {
        if (!rectangle.Intersects(arc.GetBoundingBox()))
        {
            return false;
        }

        if (rectangle.Contains(arc.StartPoint) ||
            rectangle.Contains(arc.EndPoint))
        {
            return true;
        }

        foreach (LineSegment2D edge in rectangle.GetEdges())
        {
            IReadOnlyList<Point2D> intersections =
                CircleIntersectionService.IntersectSegmentCircle(
                    edge,
                    new Circle2D(arc.Center, arc.Radius),
                    tolerance);

            if (intersections.Any(point => arc.ContainsPoint(point, tolerance)))
            {
                return true;
            }
        }

        return false;
    }
}