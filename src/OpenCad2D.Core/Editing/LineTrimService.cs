using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides trim operations for line entities.
/// </summary>
public static class LineTrimService
{
    public static IReadOnlyList<LineEntity> TrimByBoundary(
        LineEntity target,
        LineEntity boundary,
        Point2D targetPickPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(boundary);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (!LineIntersectionService.TryIntersectInfiniteLines(
                target.Geometry,
                boundary.Geometry,
                out LineIntersectionInfo intersection,
                effectiveTolerance))
        {
            return Array.Empty<LineEntity>();
        }

        if (!LineIntersectionService.IsParameterStrictlyInsideSegment(
                intersection.FirstParameter,
                effectiveTolerance) ||
            !LineIntersectionService.IsParameterOnSegment(
                intersection.SecondParameter,
                effectiveTolerance))
        {
            return Array.Empty<LineEntity>();
        }

        double pickParameter = LineParameterService.GetParameter(
            target.Geometry,
            targetPickPoint,
            effectiveTolerance);

        if (effectiveTolerance.AreParametersEqual(
                pickParameter,
                intersection.FirstParameter))
        {
            return Array.Empty<LineEntity>();
        }

        if (pickParameter < intersection.FirstParameter)
        {
            return CreateSegments(
                target,
                new[] { (intersection.Point, target.End) },
                effectiveTolerance);
        }

        return CreateSegments(
            target,
            new[] { (target.Start, intersection.Point) },
            effectiveTolerance);
    }

    private static IReadOnlyList<LineEntity> CreateSegments(
        LineEntity source,
        IEnumerable<(Point2D Start, Point2D End)> segments,
        GeometryTolerance tolerance)
    {
        var result = new List<LineEntity>();

        foreach ((Point2D start, Point2D end) in segments)
        {
            if (start.DistanceTo(end) <= tolerance.Distance)
            {
                continue;
            }

            result.Add(
                new LineEntity(
                    start,
                    end,
                    layerId: source.LayerId,
                    style: source.Style,
                    isVisible: source.IsVisible,
                    isLocked: source.IsLocked,
                    drawOrder: source.DrawOrder));
        }

        return result;
    }
}
