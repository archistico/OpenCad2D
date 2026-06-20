using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Operations;

/// <summary>
/// Provides intersection operations involving circles.
/// </summary>
public static class CircleIntersectionService
{
    /// <summary>
    /// Returns true when two circles share the same center and radius within tolerance.
    /// Coincident circles have infinitely many shared points, so point-intersection APIs
    /// intentionally keep returning an empty point list for this case.
    /// </summary>
    public static bool AreCoincident(
        Circle2D first,
        Circle2D second,
        double tolerance = Tolerance.Default)
    {
        return first.Center.DistanceTo(second.Center) <= tolerance &&
               Tolerance.AreEqual(first.Radius, second.Radius, tolerance);
    }

    public static IReadOnlyList<Point2D> IntersectLineCircle(
        Line2D line,
        Circle2D circle,
        double tolerance = Tolerance.Default)
    {
        if (Tolerance.IsZero(line.Direction.Length, tolerance))
        {
            return Array.Empty<Point2D>();
        }

        Point2D closestPoint = DistanceService.ClosestPointOnLine(
            circle.Center,
            line);

        double distance = closestPoint.DistanceTo(circle.Center);

        if (distance > circle.Radius + tolerance)
        {
            return Array.Empty<Point2D>();
        }

        if (Tolerance.AreEqual(distance, circle.Radius, tolerance))
        {
            return new[] { closestPoint };
        }

        double halfChordLength = Math.Sqrt(
            circle.Radius * circle.Radius - distance * distance);

        Vector2D direction = line.NormalizedDirection;

        Point2D first = closestPoint + direction * halfChordLength;
        Point2D second = closestPoint - direction * halfChordLength;

        return new[] { first, second };
    }

    public static IReadOnlyList<Point2D> IntersectSegmentCircle(
        LineSegment2D segment,
        Circle2D circle,
        double tolerance = Tolerance.Default)
    {
        if (Tolerance.IsZero(segment.Length, tolerance))
        {
            return Array.Empty<Point2D>();
        }

        Line2D line = Line2D.FromPoints(segment.Start, segment.End);

        IReadOnlyList<Point2D> lineIntersections = IntersectLineCircle(
            line,
            circle,
            tolerance);

        var result = new List<Point2D>();

        foreach (Point2D point in lineIntersections)
        {
            if (IsPointOnSegment(point, segment, tolerance))
            {
                AddDistinct(result, point, tolerance);
            }
        }

        return result;
    }

    public static IReadOnlyList<Point2D> IntersectArcCircle(
        Arc2D arc,
        Circle2D circle,
        double tolerance = Tolerance.Default)
    {
        var arcCircle = new Circle2D(arc.Center, arc.Radius);

        IReadOnlyList<Point2D> intersections = IntersectCircleCircle(
            arcCircle,
            circle,
            tolerance);

        return intersections
            .Where(point => arc.ContainsPoint(point, tolerance))
            .ToList();
    }

    public static IReadOnlyList<Point2D> IntersectCircleCircle(
        Circle2D first,
        Circle2D second,
        double tolerance = Tolerance.Default)
    {
        double distance = first.Center.DistanceTo(second.Center);

        if (AreCoincident(first, second, tolerance))
        {
            return Array.Empty<Point2D>();
        }

        if (distance > first.Radius + second.Radius + tolerance)
        {
            return Array.Empty<Point2D>();
        }

        if (distance < Math.Abs(first.Radius - second.Radius) - tolerance)
        {
            return Array.Empty<Point2D>();
        }

        if (Tolerance.IsZero(distance, tolerance))
        {
            return Array.Empty<Point2D>();
        }

        double a =
            (first.Radius * first.Radius
            - second.Radius * second.Radius
            + distance * distance)
            / (2.0 * distance);

        double hSquared = first.Radius * first.Radius - a * a;

        if (hSquared < -tolerance)
        {
            return Array.Empty<Point2D>();
        }

        if (hSquared < 0)
        {
            hSquared = 0;
        }

        double h = Math.Sqrt(hSquared);

        Vector2D direction = first.Center.VectorTo(second.Center).Normalize();

        Point2D basePoint = first.Center + direction * a;

        if (Tolerance.IsZero(h, tolerance))
        {
            return new[] { basePoint };
        }

        Vector2D perpendicular = direction.PerpendicularLeft();

        Point2D firstPoint = basePoint + perpendicular * h;
        Point2D secondPoint = basePoint - perpendicular * h;

        return new[] { firstPoint, secondPoint };
    }

    private static bool IsPointOnSegment(
        Point2D point,
        LineSegment2D segment,
        double tolerance)
    {
        Vector2D segmentVector = segment.Start.VectorTo(segment.End);
        Vector2D startToPoint = segment.Start.VectorTo(point);

        if (!Tolerance.IsZero(segmentVector.Cross(startToPoint), tolerance))
        {
            return false;
        }

        double dot = startToPoint.Dot(segmentVector);

        if (dot < -tolerance)
        {
            return false;
        }

        if (dot > segmentVector.LengthSquared + tolerance)
        {
            return false;
        }

        return true;
    }

    private static void AddDistinct(
        List<Point2D> points,
        Point2D point,
        double tolerance)
    {
        bool alreadyExists = points.Any(existing =>
            Tolerance.AreEqual(existing.X, point.X, tolerance) &&
            Tolerance.AreEqual(existing.Y, point.Y, tolerance));

        if (!alreadyExists)
        {
            points.Add(point);
        }
    }
}