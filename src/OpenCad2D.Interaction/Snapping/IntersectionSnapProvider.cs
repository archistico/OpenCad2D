using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides intersection snap candidates between visible CAD entities.
/// </summary>
public sealed class IntersectionSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Intersection;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        var entities = request.Document.GetVisibleEntities()
            .ToList();

        for (int firstIndex = 0; firstIndex < entities.Count; firstIndex++)
        {
            for (int secondIndex = firstIndex + 1; secondIndex < entities.Count; secondIndex++)
            {
                CadEntity first = entities[firstIndex];
                CadEntity second = entities[secondIndex];

                foreach (Point2D point in GetIntersections(first, second))
                {
                    double distance = request.CursorPoint.DistanceTo(point);

                    if (distance <= request.Tolerance)
                    {
                        yield return new SnapCandidate(
                            SnapKind.Intersection,
                            point,
                            null,
                            distance);
                    }
                }
            }
        }
    }

    private static IEnumerable<Point2D> GetIntersections(
        CadEntity first,
        CadEntity second)
    {
        return (first, second) switch
        {
            (LineEntity line1, LineEntity line2) =>
                IntersectLineLine(line1, line2),

            (LineEntity line, PolylineEntity polyline) =>
                PolylineIntersectionService.IntersectSegmentPolyline(
                    line.Geometry,
                    polyline.Geometry),

            (PolylineEntity polyline, LineEntity line) =>
                PolylineIntersectionService.IntersectSegmentPolyline(
                    line.Geometry,
                    polyline.Geometry),

            (PolylineEntity polyline1, PolylineEntity polyline2) =>
                PolylineIntersectionService.IntersectPolylinePolyline(
                    polyline1.Geometry,
                    polyline2.Geometry),

            (LineEntity line, CircleEntity circle) =>
                CircleIntersectionService.IntersectSegmentCircle(
                    line.Geometry,
                    circle.Geometry),

            (CircleEntity circle, LineEntity line) =>
                CircleIntersectionService.IntersectSegmentCircle(
                    line.Geometry,
                    circle.Geometry),

            (CircleEntity circle1, CircleEntity circle2) =>
                CircleIntersectionService.IntersectCircleCircle(
                    circle1.Geometry,
                    circle2.Geometry),

            (LineEntity line, ArcEntity arc) =>
                IntersectLineArc(line, arc),

            (ArcEntity arc, LineEntity line) =>
                IntersectLineArc(line, arc),

            (CircleEntity circle, ArcEntity arc) =>
                CircleIntersectionService.IntersectArcCircle(
                    arc.Geometry,
                    circle.Geometry),

            (ArcEntity arc, CircleEntity circle) =>
                CircleIntersectionService.IntersectArcCircle(
                    arc.Geometry,
                    circle.Geometry),

            _ => Array.Empty<Point2D>()
        };
    }

    private static IReadOnlyList<Point2D> IntersectLineLine(
        LineEntity first,
        LineEntity second)
    {
        IntersectionResult result = IntersectionService.IntersectSegments(
            first.Geometry,
            second.Geometry);

        if (result.Kind == IntersectionKind.Point &&
            result.Point.HasValue)
        {
            return new[] { result.Point.Value };
        }

        return Array.Empty<Point2D>();
    }

    private static IReadOnlyList<Point2D> IntersectLineArc(
        LineEntity line,
        ArcEntity arc)
    {
        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectSegmentCircle(
                line.Geometry,
                new Circle2D(arc.Center, arc.Radius));

        return points
            .Where(point => arc.Geometry.ContainsPoint(point))
            .ToList();
    }
}