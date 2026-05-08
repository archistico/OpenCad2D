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
            return segment.Start;

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
}