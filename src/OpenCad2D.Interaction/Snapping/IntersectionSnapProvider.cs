using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides intersection snap candidates between visible CAD entities.
/// </summary>
public sealed class IntersectionSnapProvider : ISnapProvider
{
    private const int CurveIntersectionSampleCount = 256;

    public SnapKind Kind => SnapKind.Intersection;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        var entities = request.Document.GetVisibleEntities(request.SearchArea)
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
                    ToIntersectionPolyline(polyline)),

            (PolylineEntity polyline, LineEntity line) =>
                PolylineIntersectionService.IntersectSegmentPolyline(
                    line.Geometry,
                    ToIntersectionPolyline(polyline)),

            (PolylineEntity polyline1, PolylineEntity polyline2) =>
                PolylineIntersectionService.IntersectPolylinePolyline(
                    ToIntersectionPolyline(polyline1),
                    ToIntersectionPolyline(polyline2)),

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

            (PolylineEntity polyline, CircleEntity circle) =>
                IntersectPolylineCircle(
                    ToIntersectionPolyline(polyline),
                    circle.Geometry),

            (CircleEntity circle, PolylineEntity polyline) =>
                IntersectPolylineCircle(
                    ToIntersectionPolyline(polyline),
                    circle.Geometry),

            (LineEntity line, EllipseEntity ellipse) =>
                IntersectSegmentCurveApproximation(
                    line.Geometry,
                    ToIntersectionPolyline(ellipse)),

            (EllipseEntity ellipse, LineEntity line) =>
                IntersectSegmentCurveApproximation(
                    line.Geometry,
                    ToIntersectionPolyline(ellipse)),

            (PolylineEntity polyline, EllipseEntity ellipse) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(polyline),
                    ToIntersectionPolyline(ellipse)),

            (EllipseEntity ellipse, PolylineEntity polyline) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(polyline),
                    ToIntersectionPolyline(ellipse)),

            (CircleEntity circle, EllipseEntity ellipse) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(circle),
                    ToIntersectionPolyline(ellipse)),

            (EllipseEntity ellipse, CircleEntity circle) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(circle),
                    ToIntersectionPolyline(ellipse)),

            (EllipseEntity ellipse1, EllipseEntity ellipse2) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(ellipse1),
                    ToIntersectionPolyline(ellipse2)),

            (LineEntity line, BezierSplineEntity spline) =>
                IntersectSegmentCurveApproximation(
                    line.Geometry,
                    ToIntersectionPolyline(spline)),

            (BezierSplineEntity spline, LineEntity line) =>
                IntersectSegmentCurveApproximation(
                    line.Geometry,
                    ToIntersectionPolyline(spline)),

            (PolylineEntity polyline, BezierSplineEntity spline) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(polyline),
                    ToIntersectionPolyline(spline)),

            (BezierSplineEntity spline, PolylineEntity polyline) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(polyline),
                    ToIntersectionPolyline(spline)),

            (CircleEntity circle, BezierSplineEntity spline) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(circle),
                    ToIntersectionPolyline(spline)),

            (BezierSplineEntity spline, CircleEntity circle) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(circle),
                    ToIntersectionPolyline(spline)),

            (EllipseEntity ellipse, BezierSplineEntity spline) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(ellipse),
                    ToIntersectionPolyline(spline)),

            (BezierSplineEntity spline, EllipseEntity ellipse) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(ellipse),
                    ToIntersectionPolyline(spline)),

            (BezierSplineEntity spline1, BezierSplineEntity spline2) =>
                IntersectPolylineCurveApproximation(
                    ToIntersectionPolyline(spline1),
                    ToIntersectionPolyline(spline2)),

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

            (StairEntity stair, CadEntity other) =>
                IntersectStairWithEntity(stair, other),

            (CadEntity other, StairEntity stair) =>
                IntersectStairWithEntity(stair, other),

            _ => IntersectWithCoreFallback(first, second)
        };
    }

    private static IReadOnlyList<Point2D> IntersectStairWithEntity(
        StairEntity stair,
        CadEntity other)
    {
        IReadOnlyList<LineSegment2D> stairSegments = stair.GetGeneratedGeometry().Segments;
        IReadOnlyList<LineSegment2D> otherSegments = ToIntersectionSegments(other);

        if (otherSegments.Count == 0)
        {
            return Array.Empty<Point2D>();
        }

        var intersections = new List<Point2D>();

        foreach (LineSegment2D stairSegment in stairSegments)
        {
            foreach (LineSegment2D otherSegment in otherSegments)
            {
                IntersectionResult result = IntersectionService.IntersectSegments(
                    stairSegment,
                    otherSegment);

                if (result.Kind == IntersectionKind.Point &&
                    result.Point.HasValue)
                {
                    AddDistinct(intersections, result.Point.Value);
                }
            }
        }

        return intersections;
    }

    private static IReadOnlyList<LineSegment2D> ToIntersectionSegments(CadEntity entity)
    {
        return entity switch
        {
            LineEntity line => new[] { line.Geometry },
            PolylineEntity polyline => ToIntersectionPolyline(polyline).GetSegments(),
            StairEntity stair => stair.GetGeneratedGeometry().Segments,
            CircleEntity circle => ToIntersectionPolyline(circle).GetSegments(),
            EllipseEntity ellipse => ToIntersectionPolyline(ellipse).GetSegments(),
            EllipticalArcEntity ellipticalArc =>
                new Polyline2D(
                    ellipticalArc.GetSamplePoints(CurveIntersectionSampleCount),
                    isClosed: false).GetSegments(),
            ArcEntity arc => ToIntersectionPolyline(arc).GetSegments(),
            BezierSplineEntity spline => ToIntersectionPolyline(spline).GetSegments(),
            _ => Array.Empty<LineSegment2D>()
        };
    }

    private static Polyline2D ToIntersectionPolyline(ArcEntity arc)
    {
        var points = new List<Point2D>(CurveIntersectionSampleCount + 1);

        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;
        double sweep = arc.IsCounterClockwise
            ? end - start
            : start - end;

        if (sweep < 0.0)
        {
            sweep += Math.Tau;
        }

        for (int i = 0; i <= CurveIntersectionSampleCount; i++)
        {
            double t = i / (double)CurveIntersectionSampleCount;
            double angle = arc.IsCounterClockwise
                ? start + sweep * t
                : start - sweep * t;

            points.Add(arc.Geometry.PointAt(Angle.FromRadians(angle)));
        }

        return new Polyline2D(points, isClosed: false);
    }


    private static IReadOnlyList<Point2D> IntersectWithCoreFallback(
        CadEntity first,
        CadEntity second)
    {
        return CadEntityIntersectionService.Intersect(
            first,
            second,
            GeometryTolerance.Default);
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

    private static IReadOnlyList<Point2D> IntersectSegmentCurveApproximation(
        LineSegment2D segment,
        Polyline2D curveApproximation)
    {
        return PolylineIntersectionService.IntersectSegmentPolyline(
            segment,
            curveApproximation);
    }

    private static IReadOnlyList<Point2D> IntersectPolylineCurveApproximation(
        Polyline2D polyline,
        Polyline2D curveApproximation)
    {
        return PolylineIntersectionService.IntersectPolylinePolyline(
            polyline,
            curveApproximation);
    }

    private static IReadOnlyList<Point2D> IntersectPolylineCircle(
        Polyline2D polyline,
        Circle2D circle)
    {
        var result = new List<Point2D>();

        foreach (LineSegment2D segment in polyline.GetSegments())
        {
            foreach (Point2D point in CircleIntersectionService.IntersectSegmentCircle(segment, circle))
            {
                AddDistinct(result, point);
            }
        }

        return result;
    }

    private static void AddDistinct(
        List<Point2D> points,
        Point2D point)
    {
        bool alreadyExists = points.Any(existing =>
            GeometryTolerance.Default.AreCoordinatesEqual(existing, point));

        if (!alreadyExists)
        {
            points.Add(point);
        }
    }

    private static Polyline2D ToIntersectionPolyline(PolylineEntity polyline)
    {
        return polyline.GetInteractionGeometry(CurveIntersectionSampleCount);
    }

    private static Polyline2D ToIntersectionPolyline(EllipseEntity ellipse)
    {
        return new Polyline2D(
            ellipse.GetSamplePoints(CurveIntersectionSampleCount),
            isClosed: true);
    }

    private static Polyline2D ToIntersectionPolyline(BezierSplineEntity spline)
    {
        return spline.ToPolylineApproximation(CurveIntersectionSampleCount).Geometry;
    }

    private static Polyline2D ToIntersectionPolyline(CircleEntity circle)
    {
        var points = new List<Point2D>(CurveIntersectionSampleCount);
        for (int i = 0; i < CurveIntersectionSampleCount; i++)
        {
            double angle = i * Math.Tau / CurveIntersectionSampleCount;
            points.Add(new Point2D(
                circle.Center.X + (Math.Cos(angle) * circle.Radius),
                circle.Center.Y + (Math.Sin(angle) * circle.Radius)));
        }

        return new Polyline2D(points, isClosed: true);
    }
}