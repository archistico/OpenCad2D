using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides break operations for line entities.
/// </summary>
public static class LineBreakService
{
    public static IReadOnlyList<LineEntity> BreakAtPoint(
        LineEntity line,
        Point2D breakPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(line);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        LineSegment2D segment = line.Geometry;
        double parameter = LineParameterService.GetParameter(
            segment,
            breakPoint,
            effectiveTolerance);

        if (!LineParameterService.IsStrictlyInsideSegment(parameter, effectiveTolerance))
        {
            return Array.Empty<LineEntity>();
        }

        Point2D projectedPoint = LineParameterService.PointAt(
            segment,
            parameter);

        return CreateSegments(
            line,
            new[]
            {
                (line.Start, projectedPoint),
                (projectedPoint, line.End)
            },
            effectiveTolerance);
    }

    public static IReadOnlyList<LineEntity> BreakBetweenPoints(
        LineEntity line,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(line);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        LineSegment2D segment = line.Geometry;

        double firstParameter = LineParameterService.GetParameter(
            segment,
            firstBreakPoint,
            effectiveTolerance);

        double secondParameter = LineParameterService.GetParameter(
            segment,
            secondBreakPoint,
            effectiveTolerance);

        if (!LineIntersectionService.IsParameterOnSegment(firstParameter, effectiveTolerance) ||
            !LineIntersectionService.IsParameterOnSegment(secondParameter, effectiveTolerance))
        {
            return Array.Empty<LineEntity>();
        }

        double startBreak = Math.Min(firstParameter, secondParameter);
        double endBreak = Math.Max(firstParameter, secondParameter);

        if (effectiveTolerance.AreParametersEqual(startBreak, endBreak))
        {
            return Array.Empty<LineEntity>();
        }

        Point2D firstProjectedPoint = LineParameterService.PointAt(
            segment,
            startBreak);

        Point2D secondProjectedPoint = LineParameterService.PointAt(
            segment,
            endBreak);

        return CreateSegments(
            line,
            new[]
            {
                (line.Start, firstProjectedPoint),
                (secondProjectedPoint, line.End)
            },
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
