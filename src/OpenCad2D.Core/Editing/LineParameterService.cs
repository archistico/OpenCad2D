using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides parameter-based calculations on finite line segments.
/// </summary>
public static class LineParameterService
{
    public static double GetParameter(
        LineSegment2D segment,
        Point2D point,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        Vector2D direction = segment.Start.VectorTo(segment.End);
        double lengthSquared = direction.LengthSquared;

        if (effectiveTolerance.IsVectorLengthZero(direction.Length))
        {
            throw new InvalidOperationException(
                "Cannot compute a line parameter for a zero-length segment.");
        }

        Vector2D startToPoint = segment.Start.VectorTo(point);

        return startToPoint.Dot(direction) / lengthSquared;
    }

    public static Point2D PointAt(
        LineSegment2D segment,
        double parameter)
    {
        Vector2D direction = segment.Start.VectorTo(segment.End);

        return new Point2D(
            segment.Start.X + direction.X * parameter,
            segment.Start.Y + direction.Y * parameter);
    }

    public static Point2D ProjectPointOnInfiniteLine(
        LineSegment2D segment,
        Point2D point,
        GeometryTolerance? tolerance = null)
    {
        double parameter = GetParameter(
            segment,
            point,
            tolerance);

        return PointAt(
            segment,
            parameter);
    }

    public static bool IsStrictlyInsideSegment(
        double parameter,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return parameter > effectiveTolerance.Parameter &&
               parameter < 1.0 - effectiveTolerance.Parameter;
    }
}
