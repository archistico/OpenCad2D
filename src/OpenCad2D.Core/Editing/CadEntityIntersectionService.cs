using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides intersection helpers for editable CAD entities.
/// </summary>
public static class CadEntityIntersectionService
{
    public static IReadOnlyList<Point2D> Intersect(
        CadEntity first,
        CadEntity second,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        double distanceTolerance = effectiveTolerance.Distance;

        var result = new List<Point2D>();

        foreach (EntitySegment firstSegment in ExplodeForIntersection(first))
        {
            foreach (EntitySegment secondSegment in ExplodeForIntersection(second))
            {
                foreach (Point2D point in IntersectSegments(
                             firstSegment,
                             secondSegment,
                             distanceTolerance))
                {
                    AddDistinct(result, point, distanceTolerance);
                }
            }
        }

        return result;
    }

    public static IReadOnlyList<Point2D> IntersectInfiniteLineWithEntity(
        Line2D infiniteLine,
        CadEntity entity,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        double distanceTolerance = effectiveTolerance.Distance;
        var result = new List<Point2D>();

        foreach (EntitySegment segment in ExplodeForIntersection(entity))
        {
            IReadOnlyList<Point2D> points = segment.Kind switch
            {
                EntitySegmentKind.Line => IntersectInfiniteLineWithSegment(
                    infiniteLine,
                    segment.Line,
                    effectiveTolerance),

                EntitySegmentKind.Circle => CircleIntersectionService.IntersectLineCircle(
                    infiniteLine,
                    segment.Circle,
                    distanceTolerance),

                EntitySegmentKind.Arc => CircleIntersectionService.IntersectLineCircle(
                        infiniteLine,
                        new Circle2D(segment.Arc.Center, segment.Arc.Radius),
                        distanceTolerance)
                    .Where(point => segment.Arc.ContainsPoint(point, distanceTolerance))
                    .ToList(),

                _ => Array.Empty<Point2D>()
            };

            foreach (Point2D point in points)
            {
                AddDistinct(result, point, distanceTolerance);
            }
        }

        return result;
    }

    public static IReadOnlyList<Point2D> IntersectCircleWithEntity(
        Circle2D circle,
        CadEntity entity,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        double distanceTolerance = effectiveTolerance.Distance;
        var result = new List<Point2D>();

        foreach (EntitySegment segment in ExplodeForIntersection(entity))
        {
            IReadOnlyList<Point2D> points = segment.Kind switch
            {
                EntitySegmentKind.Line => CircleIntersectionService.IntersectSegmentCircle(
                    segment.Line,
                    circle,
                    distanceTolerance),

                EntitySegmentKind.Circle => CircleIntersectionService.IntersectCircleCircle(
                    circle,
                    segment.Circle,
                    distanceTolerance),

                EntitySegmentKind.Arc => CircleIntersectionService.IntersectCircleCircle(
                        circle,
                        new Circle2D(segment.Arc.Center, segment.Arc.Radius),
                        distanceTolerance)
                    .Where(point => segment.Arc.ContainsPoint(point, distanceTolerance))
                    .ToList(),

                _ => Array.Empty<Point2D>()
            };

            foreach (Point2D point in points)
            {
                AddDistinct(result, point, distanceTolerance);
            }
        }

        return result;
    }

    public static bool IsPointOnEntity(
        CadEntity entity,
        Point2D point,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return entity.DistanceTo(point) <= effectiveTolerance.Distance;
    }

    private static IEnumerable<EntitySegment> ExplodeForIntersection(CadEntity entity)
    {
        switch (entity)
        {
            case LineEntity line:
                yield return EntitySegment.FromLine(line.Geometry);
                break;

            case CircleEntity circle:
                yield return EntitySegment.FromCircle(circle.Geometry);
                break;

            case ArcEntity arc:
                yield return EntitySegment.FromArc(arc.Geometry);
                break;

            case PolylineEntity polyline:
                foreach (LineSegment2D segment in polyline.Geometry.GetSegments())
                {
                    yield return EntitySegment.FromLine(segment);
                }

                break;
        }
    }

    private static IReadOnlyList<Point2D> IntersectSegments(
        EntitySegment first,
        EntitySegment second,
        double tolerance)
    {
        if (first.Kind == EntitySegmentKind.Line &&
            second.Kind == EntitySegmentKind.Line)
        {
            IntersectionResult intersection = IntersectionService.IntersectSegments(
                first.Line,
                second.Line,
                tolerance);

            return intersection.Kind == IntersectionKind.Point && intersection.Point.HasValue
                ? new[] { intersection.Point.Value }
                : Array.Empty<Point2D>();
        }

        if (first.Kind == EntitySegmentKind.Line &&
            second.Kind == EntitySegmentKind.Circle)
        {
            return CircleIntersectionService.IntersectSegmentCircle(
                first.Line,
                second.Circle,
                tolerance);
        }

        if (first.Kind == EntitySegmentKind.Circle &&
            second.Kind == EntitySegmentKind.Line)
        {
            return CircleIntersectionService.IntersectSegmentCircle(
                second.Line,
                first.Circle,
                tolerance);
        }

        if (first.Kind == EntitySegmentKind.Line &&
            second.Kind == EntitySegmentKind.Arc)
        {
            return CircleIntersectionService.IntersectSegmentCircle(
                    first.Line,
                    new Circle2D(second.Arc.Center, second.Arc.Radius),
                    tolerance)
                .Where(point => second.Arc.ContainsPoint(point, tolerance))
                .ToList();
        }

        if (first.Kind == EntitySegmentKind.Arc &&
            second.Kind == EntitySegmentKind.Line)
        {
            return CircleIntersectionService.IntersectSegmentCircle(
                    second.Line,
                    new Circle2D(first.Arc.Center, first.Arc.Radius),
                    tolerance)
                .Where(point => first.Arc.ContainsPoint(point, tolerance))
                .ToList();
        }

        if (first.Kind == EntitySegmentKind.Circle &&
            second.Kind == EntitySegmentKind.Circle)
        {
            return CircleIntersectionService.IntersectCircleCircle(
                first.Circle,
                second.Circle,
                tolerance);
        }

        if (first.Kind == EntitySegmentKind.Circle &&
            second.Kind == EntitySegmentKind.Arc)
        {
            return CircleIntersectionService.IntersectCircleCircle(
                    first.Circle,
                    new Circle2D(second.Arc.Center, second.Arc.Radius),
                    tolerance)
                .Where(point => second.Arc.ContainsPoint(point, tolerance))
                .ToList();
        }

        if (first.Kind == EntitySegmentKind.Arc &&
            second.Kind == EntitySegmentKind.Circle)
        {
            return CircleIntersectionService.IntersectCircleCircle(
                    new Circle2D(first.Arc.Center, first.Arc.Radius),
                    second.Circle,
                    tolerance)
                .Where(point => first.Arc.ContainsPoint(point, tolerance))
                .ToList();
        }

        if (first.Kind == EntitySegmentKind.Arc &&
            second.Kind == EntitySegmentKind.Arc)
        {
            return CircleIntersectionService.IntersectCircleCircle(
                    new Circle2D(first.Arc.Center, first.Arc.Radius),
                    new Circle2D(second.Arc.Center, second.Arc.Radius),
                    tolerance)
                .Where(point =>
                    first.Arc.ContainsPoint(point, tolerance) &&
                    second.Arc.ContainsPoint(point, tolerance))
                .ToList();
        }

        return Array.Empty<Point2D>();
    }

    private static IReadOnlyList<Point2D> IntersectInfiniteLineWithSegment(
        Line2D infiniteLine,
        LineSegment2D segment,
        GeometryTolerance tolerance)
    {
        Vector2D p = infiniteLine.Point.VectorTo(segment.Start);
        Vector2D r = infiniteLine.Direction;
        Vector2D s = segment.Start.VectorTo(segment.End);
        double cross = r.Cross(s);

        if (tolerance.IsDistanceZero(cross) || tolerance.IsVectorLengthZero(s.Length))
        {
            return Array.Empty<Point2D>();
        }

        double u = p.Cross(r) / cross;

        if (!tolerance.IsParameterWithinUnitInterval(u))
        {
            return Array.Empty<Point2D>();
        }

        return new[]
        {
            new Point2D(
                segment.Start.X + s.X * u,
                segment.Start.Y + s.Y * u)
        };
    }

    private static void AddDistinct(
        List<Point2D> points,
        Point2D point,
        double tolerance)
    {
        if (points.Any(existing => existing.DistanceTo(point) <= tolerance))
        {
            return;
        }

        points.Add(point);
    }

    private enum EntitySegmentKind
    {
        Line,
        Circle,
        Arc
    }

    private readonly record struct EntitySegment(
        EntitySegmentKind Kind,
        LineSegment2D Line,
        Circle2D Circle,
        Arc2D Arc)
    {
        public static EntitySegment FromLine(LineSegment2D line) =>
            new(EntitySegmentKind.Line, line, default, default);

        public static EntitySegment FromCircle(Circle2D circle) =>
            new(EntitySegmentKind.Circle, default, circle, default);

        public static EntitySegment FromArc(Arc2D arc) =>
            new(EntitySegmentKind.Arc, default, default, arc);
    }
}
