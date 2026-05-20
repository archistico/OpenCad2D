using OpenCad2D.Core.Editing.Curves;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides break operations for supported CAD entities.
/// </summary>
public static class CadBreakService
{
    private static readonly CadCurveSplitService CurveSplitService = new();

    public static IReadOnlyList<CadEntity> BreakAtPoint(
        CadEntity entity,
        Point2D breakPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return entity switch
        {
            LineEntity or ArcEntity or PolylineEntity or EllipticalArcEntity or BezierSplineEntity => CurveSplitService.SplitAtPoint(
                entity,
                breakPoint,
                effectiveTolerance),

            // A one-point break on a full closed conic is intentionally kept as a no-op for now.
            // The stable user-facing workflow is Break Between Points, which creates a native
            // open arc without introducing a near-360-degree arc special case.
            CircleEntity or EllipseEntity => Array.Empty<CadEntity>(),

            _ => Array.Empty<CadEntity>()
        };
    }

    public static IReadOnlyList<CadEntity> BreakBetweenPoints(
        CadEntity entity,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return entity switch
        {
            LineEntity or CircleEntity or ArcEntity or PolylineEntity or EllipseEntity or EllipticalArcEntity or BezierSplineEntity => CurveSplitService.RemoveBetweenPoints(
                entity,
                firstBreakPoint,
                secondBreakPoint,
                effectiveTolerance),

            _ => Array.Empty<CadEntity>()
        };
    }

    public static IReadOnlyList<CadEntity> GetRemovedSegmentBetweenPoints(
        CadEntity entity,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return entity switch
        {
            LineEntity or CircleEntity or ArcEntity or PolylineEntity or EllipseEntity or EllipticalArcEntity or BezierSplineEntity => CurveSplitService.GetIntervalBetweenPoints(
                entity,
                firstBreakPoint,
                secondBreakPoint,
                effectiveTolerance),

            _ => Array.Empty<CadEntity>()
        };
    }
}
