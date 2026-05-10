using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides line-line intersection calculations with explicit parameters.
/// </summary>
public static class LineIntersectionService
{
    public static bool TryIntersectInfiniteLines(
        LineSegment2D first,
        LineSegment2D second,
        out LineIntersectionInfo intersection,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        Point2D p = first.Start;
        Point2D q = second.Start;

        Vector2D r = first.Start.VectorTo(first.End);
        Vector2D s = second.Start.VectorTo(second.End);

        intersection = default;

        if (effectiveTolerance.IsVectorLengthZero(r.Length) ||
            effectiveTolerance.IsVectorLengthZero(s.Length))
        {
            return false;
        }

        double rCrossS = r.Cross(s);

        if (effectiveTolerance.IsDistanceZero(rCrossS))
        {
            return false;
        }

        Vector2D qMinusP = p.VectorTo(q);

        double firstParameter = qMinusP.Cross(s) / rCrossS;
        double secondParameter = qMinusP.Cross(r) / rCrossS;

        Point2D point = new(
            p.X + firstParameter * r.X,
            p.Y + firstParameter * r.Y);

        intersection = new LineIntersectionInfo(
            point,
            firstParameter,
            secondParameter);

        return true;
    }

    public static bool IsParameterOnSegment(
        double parameter,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return effectiveTolerance.IsParameterWithinUnitInterval(parameter);
    }

    public static bool IsParameterStrictlyInsideSegment(
        double parameter,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return parameter > effectiveTolerance.Parameter &&
               parameter < 1.0 - effectiveTolerance.Parameter;
    }
}
