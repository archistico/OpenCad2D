using OpenCad2D.Core.Editing.Curves;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides trim operations for the editable entity types supported by OpenCad2D.
/// </summary>
public static class CadTrimService
{
    private static readonly CadCurveSplitService CurveSplitService = new();

    public static IReadOnlyList<CadEntity> TrimByBoundary(
        CadEntity target,
        CadEntity boundary,
        Point2D targetPickPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        return TrimByBoundaries(
            target,
            new[] { boundary },
            targetPickPoint,
            tolerance);
    }

    /// <summary>
    /// Trims the target entity against one or more boundary entities.
    /// </summary>
    public static IReadOnlyList<CadEntity> TrimByBoundaries(
        CadEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D targetPickPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(boundaries);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (boundaries.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        return target switch
        {
            BezierSplineEntity spline => TrimBezierSplineByBoundaries(
                spline,
                boundaries,
                targetPickPoint,
                effectiveTolerance),

            LineEntity line => TrimLineByBoundaries(
                line,
                boundaries,
                targetPickPoint,
                effectiveTolerance),

            CircleEntity circle => TrimCircleByBoundaries(
                circle,
                boundaries,
                targetPickPoint,
                effectiveTolerance),

            ArcEntity arc => TrimArcByBoundaries(
                arc,
                boundaries,
                targetPickPoint,
                effectiveTolerance),

            EllipseEntity ellipse => TrimEllipseByBoundaries(
                ellipse,
                boundaries,
                targetPickPoint,
                effectiveTolerance),

            EllipticalArcEntity ellipticalArc => TrimEllipticalArcByBoundaries(
                ellipticalArc,
                boundaries,
                targetPickPoint,
                effectiveTolerance),

            PolylineEntity polyline => TrimPolylineByBoundaries(
                polyline,
                boundaries,
                targetPickPoint,
                effectiveTolerance),

            _ => Array.Empty<CadEntity>()
        };
    }


    /// <summary>
    /// Builds the native curve interval that would be removed by a TRIM operation.
    /// This is intended for transient command previews and uses the same cut collection
    /// and native curve splitting pipeline as <see cref="TrimByBoundaries"/>.
    /// </summary>
    public static IReadOnlyList<CadEntity> GetRemovedIntervalByBoundaries(
        CadEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D targetPickPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(boundaries);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (boundaries.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        IReadOnlyList<Point2D> intersections = CollectTrimIntersections(
            target,
            boundaries,
            effectiveTolerance);

        return CurveSplitService.GetPickedInterval(
            target,
            intersections,
            targetPickPoint,
            effectiveTolerance);
    }

    private static IReadOnlyList<CadEntity> TrimLineByBoundaries(
        LineEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CollectTrimIntersections(
            target,
            boundaries,
            tolerance);

        return CurveSplitService.RemovePickedInterval(
            target,
            intersections,
            pickPoint,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimCircleByBoundaries(
        CircleEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CollectIntersections(
            target,
            boundaries,
            tolerance);

        return CurveSplitService.RemovePickedInterval(
            target,
            intersections,
            pickPoint,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimArcByBoundaries(
        ArcEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CollectTrimIntersections(
            target,
            boundaries,
            tolerance);

        return CurveSplitService.RemovePickedInterval(
            target,
            intersections,
            pickPoint,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimEllipseByBoundaries(
        EllipseEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CollectIntersections(
            target,
            boundaries,
            tolerance);

        return CurveSplitService.RemovePickedInterval(
            target,
            intersections,
            pickPoint,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimEllipticalArcByBoundaries(
        EllipticalArcEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CollectTrimIntersections(
            target,
            boundaries,
            tolerance);

        return CurveSplitService.RemovePickedInterval(
            target,
            intersections,
            pickPoint,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimBezierSplineByBoundaries(
        BezierSplineEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        if (target.IsClosed)
        {
            return Array.Empty<CadEntity>();
        }

        IReadOnlyList<Point2D> intersections = CollectTrimIntersections(
            target,
            boundaries,
            tolerance);

        return CurveSplitService.RemovePickedInterval(
            target,
            intersections,
            pickPoint,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimPolylineByBoundaries(
        PolylineEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CollectTrimIntersections(
            target,
            boundaries,
            tolerance);

        return CurveSplitService.RemovePickedInterval(
            target,
            intersections,
            pickPoint,
            tolerance);
    }


    private static IReadOnlyList<Point2D> CollectTrimIntersections(
        CadEntity target,
        IReadOnlyList<CadEntity> boundaries,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CollectIntersections(
            target,
            boundaries,
            tolerance);

        return target switch
        {
            LineEntity line => intersections
                .Where(point => !tolerance.ArePointsEqual(point, line.Start) &&
                                !tolerance.ArePointsEqual(point, line.End))
                .ToList(),

            ArcEntity arc => intersections
                .Where(point => !tolerance.ArePointsEqual(point, arc.Geometry.StartPoint) &&
                                !tolerance.ArePointsEqual(point, arc.Geometry.EndPoint))
                .ToList(),

            EllipticalArcEntity ellipticalArc => intersections
                .Where(point => !tolerance.ArePointsEqual(point, ellipticalArc.StartPoint) &&
                                !tolerance.ArePointsEqual(point, ellipticalArc.EndPoint))
                .ToList(),

            BezierSplineEntity spline when !spline.IsClosed => intersections
                .Where(point => !tolerance.ArePointsEqual(point, spline.ControlPoints[0]) &&
                                !tolerance.ArePointsEqual(point, spline.ControlPoints[^1]))
                .ToList(),

            PolylineEntity polyline when !polyline.IsClosed && polyline.Vertices.Count > 0 => intersections
                .Where(point => !tolerance.ArePointsEqual(point, polyline.Vertices[0]) &&
                                !tolerance.ArePointsEqual(point, polyline.Vertices[^1]))
                .ToList(),

            _ => intersections
        };
    }

    private static IReadOnlyList<Point2D> CollectIntersections(
        CadEntity target,
        IReadOnlyList<CadEntity> boundaries,
        GeometryTolerance tolerance)
    {
        var intersections = new List<Point2D>();

        foreach (CadEntity boundary in boundaries)
        {
            intersections.AddRange(CadEntityIntersectionService.Intersect(
                target,
                boundary,
                tolerance));
        }

        return intersections;
    }
}
