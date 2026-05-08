using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Operations;

/// <summary>
/// Provides distance and closest-point operations for 2D geometric primitives.
/// </summary>
public static class DistanceService
{
    public static Point2D ClosestPointOnSegment(
        Point2D point,
        LineSegment2D segment)
    {
        Vector2D segmentVector = segment.Start.VectorTo(segment.End);
        double lengthSquared = segmentVector.LengthSquared;

        if (Tolerance.IsZero(lengthSquared))
        {
            return segment.Start;
        }

        Vector2D startToPoint = segment.Start.VectorTo(point);

        double t = startToPoint.Dot(segmentVector) / lengthSquared;
        t = Math.Clamp(t, 0.0, 1.0);

        return new Point2D(
            segment.Start.X + segmentVector.X * t,
            segment.Start.Y + segmentVector.Y * t);
    }

    public static double DistancePointToSegment(
        Point2D point,
        LineSegment2D segment)
    {
        Point2D closestPoint = ClosestPointOnSegment(point, segment);

        return point.DistanceTo(closestPoint);
    }

    public static Point2D ClosestPointOnLine(
        Point2D point,
        Line2D line)
    {
        Vector2D direction = line.NormalizedDirection;
        Vector2D lineToPoint = line.Point.VectorTo(point);

        double projection = lineToPoint.Dot(direction);

        return new Point2D(
            line.Point.X + direction.X * projection,
            line.Point.Y + direction.Y * projection);
    }

    public static double DistancePointToLine(
        Point2D point,
        Line2D line)
    {
        Point2D closestPoint = ClosestPointOnLine(point, line);

        return point.DistanceTo(closestPoint);
    }

    public static Point2D ClosestPointOnCircle(
        Point2D point,
        Circle2D circle)
    {
        Vector2D centerToPoint = circle.Center.VectorTo(point);

        if (Tolerance.IsZero(centerToPoint.Length))
        {
            return new Point2D(
                circle.Center.X + circle.Radius,
                circle.Center.Y);
        }

        Vector2D direction = centerToPoint.Normalize();

        return circle.Center + direction * circle.Radius;
    }

    public static double DistancePointToCircle(
        Point2D point,
        Circle2D circle)
    {
        Point2D closestPoint = ClosestPointOnCircle(point, circle);

        return point.DistanceTo(closestPoint);
    }

    public static Point2D ClosestPointOnArc(
        Point2D point,
        Arc2D arc)
    {
        Vector2D centerToPoint = arc.Center.VectorTo(point);

        if (Tolerance.IsZero(centerToPoint.Length))
        {
            return arc.StartPoint;
        }

        double radians = Math.Atan2(
            point.Y - arc.Center.Y,
            point.X - arc.Center.X);

        Angle projectedAngle = Angle.FromRadians(radians);

        if (arc.ContainsAngle(projectedAngle))
        {
            return arc.PointAt(projectedAngle);
        }

        double distanceToStart = point.DistanceTo(arc.StartPoint);
        double distanceToEnd = point.DistanceTo(arc.EndPoint);

        return distanceToStart <= distanceToEnd
            ? arc.StartPoint
            : arc.EndPoint;
    }

    public static double DistancePointToArc(
        Point2D point,
        Arc2D arc)
    {
        Point2D closestPoint = ClosestPointOnArc(point, arc);

        return point.DistanceTo(closestPoint);
    }

    public static Point2D ClosestPointOnPolyline(
    Point2D point,
    Polyline2D polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        IReadOnlyList<LineSegment2D> segments = polyline.GetSegments();

        Point2D closestPoint = ClosestPointOnSegment(point, segments[0]);
        double bestDistance = point.DistanceTo(closestPoint);

        for (int index = 1; index < segments.Count; index++)
        {
            Point2D candidate = ClosestPointOnSegment(point, segments[index]);
            double candidateDistance = point.DistanceTo(candidate);

            if (candidateDistance < bestDistance)
            {
                closestPoint = candidate;
                bestDistance = candidateDistance;
            }
        }

        return closestPoint;
    }

    public static double DistancePointToPolyline(
        Point2D point,
        Polyline2D polyline)
    {
        Point2D closestPoint = ClosestPointOnPolyline(point, polyline);

        return point.DistanceTo(closestPoint);
    }
}