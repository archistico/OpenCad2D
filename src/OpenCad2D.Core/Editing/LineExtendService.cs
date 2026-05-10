using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides extend operations for line entities.
/// </summary>
public static class LineExtendService
{
    public static LineEntity? ExtendToBoundary(
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
            return null;
        }

        if (!LineIntersectionService.IsParameterOnSegment(
                intersection.SecondParameter,
                effectiveTolerance))
        {
            return null;
        }

        bool pickedStart = targetPickPoint.DistanceTo(target.Start) <=
                           targetPickPoint.DistanceTo(target.End);

        if (pickedStart)
        {
            if (intersection.FirstParameter >= -effectiveTolerance.Parameter)
            {
                return null;
            }

            return new LineEntity(
                intersection.Point,
                target.End,
                target.Id,
                target.LayerId,
                target.Style,
                target.IsVisible,
                target.IsLocked,
                target.DrawOrder);
        }

        if (intersection.FirstParameter <= 1.0 + effectiveTolerance.Parameter)
        {
            return null;
        }

        return new LineEntity(
            target.Start,
            intersection.Point,
            target.Id,
            target.LayerId,
            target.Style,
            target.IsVisible,
            target.IsLocked,
            target.DrawOrder);
    }
}
