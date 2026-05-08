using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Operations;

/// <summary>
/// Provides intersection operations for 2D geometric primitives.
/// </summary>
public static class IntersectionService
{
    public static IntersectionResult IntersectSegments(
        LineSegment2D first,
        LineSegment2D second,
        double tolerance = Tolerance.Default)
    {
        Point2D p = first.Start;
        Point2D q = second.Start;

        Vector2D r = first.Start.VectorTo(first.End);
        Vector2D s = second.Start.VectorTo(second.End);

        double rCrossS = r.Cross(s);
        Vector2D qMinusP = p.VectorTo(q);
        double qMinusPCrossR = qMinusP.Cross(r);

        if (Tolerance.IsZero(r.LengthSquared, tolerance) ||
            Tolerance.IsZero(s.LengthSquared, tolerance))
        {
            return IntersectionResult.None;
        }

        if (Tolerance.IsZero(rCrossS, tolerance) &&
            Tolerance.IsZero(qMinusPCrossR, tolerance))
        {
            return IntersectCollinearSegments(first, second, tolerance);
        }

        if (Tolerance.IsZero(rCrossS, tolerance) &&
            !Tolerance.IsZero(qMinusPCrossR, tolerance))
        {
            return IntersectionResult.None;
        }

        double t = qMinusP.Cross(s) / rCrossS;
        double u = qMinusP.Cross(r) / rCrossS;

        if (IsWithinUnitInterval(t, tolerance) &&
            IsWithinUnitInterval(u, tolerance))
        {
            Point2D intersectionPoint = new(
                p.X + t * r.X,
                p.Y + t * r.Y);

            return IntersectionResult.SinglePoint(intersectionPoint);
        }

        return IntersectionResult.None;
    }

    private static IntersectionResult IntersectCollinearSegments(
        LineSegment2D first,
        LineSegment2D second,
        double tolerance)
    {
        bool firstStartOnSecond = IsPointOnSegment(first.Start, second, tolerance);
        bool firstEndOnSecond = IsPointOnSegment(first.End, second, tolerance);
        bool secondStartOnFirst = IsPointOnSegment(second.Start, first, tolerance);
        bool secondEndOnFirst = IsPointOnSegment(second.End, first, tolerance);

        int count = 0;
        Point2D? lastPoint = null;

        AddIfOnSegment(first.Start, firstStartOnSecond, ref count, ref lastPoint);
        AddIfOnSegment(first.End, firstEndOnSecond, ref count, ref lastPoint);
        AddIfOnSegment(second.Start, secondStartOnFirst, ref count, ref lastPoint);
        AddIfOnSegment(second.End, secondEndOnFirst, ref count, ref lastPoint);

        if (count == 0)
            return IntersectionResult.None;

        if (count == 1 && lastPoint.HasValue)
            return IntersectionResult.SinglePoint(lastPoint.Value);

        return IntersectionResult.Overlapping();
    }

    private static void AddIfOnSegment(
        Point2D point,
        bool isOnSegment,
        ref int count,
        ref Point2D? lastPoint)
    {
        if (!isOnSegment)
            return;

        if (lastPoint.HasValue && AreSamePoint(lastPoint.Value, point))
            return;

        count++;
        lastPoint = point;
    }

    private static bool IsPointOnSegment(
        Point2D point,
        LineSegment2D segment,
        double tolerance)
    {
        Vector2D segmentVector = segment.Start.VectorTo(segment.End);
        Vector2D startToPoint = segment.Start.VectorTo(point);

        if (!Tolerance.IsZero(segmentVector.Cross(startToPoint), tolerance))
            return false;

        double dot = startToPoint.Dot(segmentVector);

        if (dot < -tolerance)
            return false;

        if (dot > segmentVector.LengthSquared + tolerance)
            return false;

        return true;
    }

    private static bool IsWithinUnitInterval(double value, double tolerance)
    {
        return value >= -tolerance && value <= 1.0 + tolerance;
    }

    private static bool AreSamePoint(Point2D first, Point2D second)
    {
        return Tolerance.AreEqual(first.X, second.X)
            && Tolerance.AreEqual(first.Y, second.Y);
    }
}