using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Operations;

/// <summary>
/// Provides intersection operations involving polylines.
/// </summary>
public static class PolylineIntersectionService
{
    public static IReadOnlyList<Point2D> IntersectSegmentPolyline(
        LineSegment2D segment,
        Polyline2D polyline,
        double tolerance = Tolerance.Default)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        var result = new List<Point2D>();

        foreach (LineSegment2D polylineSegment in polyline.GetSegments())
        {
            IntersectionResult intersection =
                IntersectionService.IntersectSegments(
                    segment,
                    polylineSegment,
                    tolerance);

            if (intersection.Kind == IntersectionKind.Point &&
                intersection.Point.HasValue)
            {
                AddDistinct(result, intersection.Point.Value, tolerance);
            }
        }

        return result;
    }

    public static IReadOnlyList<Point2D> IntersectPolylinePolyline(
        Polyline2D first,
        Polyline2D second,
        double tolerance = Tolerance.Default)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var result = new List<Point2D>();

        foreach (LineSegment2D firstSegment in first.GetSegments())
        {
            foreach (LineSegment2D secondSegment in second.GetSegments())
            {
                IntersectionResult intersection =
                    IntersectionService.IntersectSegments(
                        firstSegment,
                        secondSegment,
                        tolerance);

                if (intersection.Kind == IntersectionKind.Point &&
                    intersection.Point.HasValue)
                {
                    AddDistinct(result, intersection.Point.Value, tolerance);
                }
            }
        }

        return result;
    }

    public static bool IntersectsSegmentPolyline(
        LineSegment2D segment,
        Polyline2D polyline,
        double tolerance = Tolerance.Default)
    {
        return IntersectSegmentPolyline(segment, polyline, tolerance).Count > 0;
    }

    public static bool IntersectsPolylinePolyline(
        Polyline2D first,
        Polyline2D second,
        double tolerance = Tolerance.Default)
    {
        return IntersectPolylinePolyline(first, second, tolerance).Count > 0;
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