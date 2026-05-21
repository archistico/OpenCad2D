using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Editing;

internal static class EditingStatusMessageBuilder
{
    public static string BuildTrimFailureMessage(
        CadEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(boundaries);

        if (target is BezierSplineEntity { IsClosed: true })
        {
            return "Closed splines cannot be trimmed yet. Use an open spline or explode/convert the curve before trimming.";
        }

        int intersectionCount = CountIntersections(target, boundaries, tolerance);

        if (intersectionCount == 0)
        {
            return boundaries.Count == 1
                ? $"No trim intersection found between the selected {GetEntityName(target)} and the cutting edge. Pick an entity that crosses the cutting edge."
                : $"No trim intersection found between the selected {GetEntityName(target)} and the active cutting edges. Add a crossing cutting edge or use All with visible crossing geometry.";
        }

        return boundaries.Count == 1
            ? "A trim intersection exists, but the picked side does not produce a removable interval. Pick the side that crosses the cutting edge, away from endpoints and intersection points."
            : "Trim intersections exist, but the picked side does not produce a removable interval. Pick a side between two cutting edges or outside the selected cutting-edge range.";
    }

    public static string BuildBreakAtPointFailureMessage(
        CadEntity target,
        Point2D breakPoint,
        GeometryTolerance tolerance)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target is BezierSplineEntity { IsClosed: true })
        {
            return "Break Point does not support closed splines yet. Use Break Segment with two points or an open spline.";
        }

        Point2D projectedPoint = target.GetClosestPoint(breakPoint);
        double distance = projectedPoint.DistanceTo(breakPoint);

        if (distance > tolerance.Distance)
        {
            return $"Break point is not on the selected {GetEntityName(target)}. Pick directly on the entity or enable object snaps.";
        }

        return target switch
        {
            LineEntity or ArcEntity or EllipticalArcEntity =>
                "Break point is too close to an endpoint or intersection tolerance. Pick an interior point on the entity.",

            PolylineEntity =>
                "Break point is too close to a polyline vertex or endpoint. Pick inside a segment, away from vertices.",

            BezierSplineEntity =>
                "Break point is too close to the spline endpoint or cannot be projected onto a stable spline segment. Pick an interior point on the open spline.",

            _ =>
                "Break point could not split the selected entity. Pick an interior point on a supported editable curve."
        };
    }

    public static string BuildBreakBetweenPointsFailureMessage(
        CadEntity target,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance tolerance)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (firstBreakPoint.DistanceTo(secondBreakPoint) <= tolerance.Distance)
        {
            return "Break points are too close together. Pick two distinct points on the entity.";
        }

        Point2D projectedSecondPoint = target.GetClosestPoint(secondBreakPoint);
        double distance = projectedSecondPoint.DistanceTo(secondBreakPoint);

        if (distance > tolerance.Distance)
        {
            return $"Second break point is not on the selected {GetEntityName(target)}. Pick directly on the entity or enable object snaps.";
        }

        return target switch
        {
            PolylineEntity =>
                "The selected polyline segment could not be removed. Pick two points on the same continuous polyline path and away from vertices.",

            CircleEntity or EllipseEntity =>
                "The selected closed curve segment could not be removed. Pick two distinct points on the curve; point order defines the removed side.",

            BezierSplineEntity { IsClosed: true } =>
                "Closed spline segment removal is not supported yet. Use an open spline or explode/convert the curve before breaking it.",

            BezierSplineEntity =>
                "The selected spline segment could not be removed. Pick two stable points on the open spline, away from endpoints.",

            _ =>
                "The selected entity segment could not be removed. Pick two distinct points on the same editable curve."
        };
    }

    public static string BuildExtendFailureMessage(
        CadEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(boundary);

        int intersectionCount = CountExtendCandidateIntersections(
            target,
            boundary,
            tolerance);

        if (intersectionCount == 0)
        {
            return $"No extension possible: the projected {GetEntityName(target)} does not intersect the selected {GetEntityName(boundary)} boundary.";
        }

        string endpointHint = target switch
        {
            LineEntity => "line endpoint",
            ArcEntity or EllipticalArcEntity => "arc endpoint",
            PolylineEntity => "polyline endpoint",
            _ => "endpoint"
        };

        return $"The boundary intersects the projected {GetEntityName(target)}, but not beyond the picked {endpointHint}. Pick the opposite endpoint side or choose a farther boundary.";
    }

    private static int CountIntersections(
        CadEntity target,
        IReadOnlyList<CadEntity> boundaries,
        GeometryTolerance tolerance)
    {
        int count = 0;

        foreach (CadEntity boundary in boundaries)
        {
            count += CadEntityIntersectionService.Intersect(
                    target,
                    boundary,
                    tolerance)
                .Count;
        }

        return count;
    }

    private static int CountExtendCandidateIntersections(
        CadEntity target,
        CadEntity boundary,
        GeometryTolerance tolerance)
    {
        return target switch
        {
            LineEntity line => CadEntityIntersectionService
                .IntersectInfiniteLineWithEntity(
                    Line2D.FromPoints(line.Start, line.End),
                    boundary,
                    tolerance)
                .Count,

            ArcEntity arc => CadEntityIntersectionService
                .IntersectCircleWithEntity(
                    new Circle2D(arc.Center, arc.Radius),
                    boundary,
                    tolerance)
                .Count,

            EllipticalArcEntity ellipticalArc => CadEntityIntersectionService
                .Intersect(
                    new EllipseEntity(
                        ellipticalArc.Center,
                        ellipticalArc.MajorAxis,
                        ellipticalArc.MinorRadius),
                    boundary,
                    tolerance)
                .Count,

            PolylineEntity polyline when !polyline.IsClosed && polyline.Vertices.Count >= 2 =>
                CountOpenPolylineEndpointCandidateIntersections(polyline, boundary, tolerance),

            _ => 0
        };
    }

    private static int CountOpenPolylineEndpointCandidateIntersections(
        PolylineEntity polyline,
        CadEntity boundary,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> vertices = polyline.Vertices;
        Point2D start = vertices[0];
        Point2D next = vertices[1];
        Point2D beforeEnd = vertices[^2];
        Point2D end = vertices[^1];

        int startCount = CadEntityIntersectionService
            .IntersectInfiniteLineWithEntity(
                Line2D.FromPoints(next, start),
                boundary,
                tolerance)
            .Count;

        int endCount = CadEntityIntersectionService
            .IntersectInfiniteLineWithEntity(
                Line2D.FromPoints(beforeEnd, end),
                boundary,
                tolerance)
            .Count;

        return startCount + endCount;
    }

    private static string GetEntityName(CadEntity entity)
    {
        return entity switch
        {
            LineEntity => "line",
            CircleEntity => "circle",
            ArcEntity => "arc",
            EllipseEntity => "ellipse",
            EllipticalArcEntity => "elliptical arc",
            PolylineEntity { IsClosed: true } => "closed polyline",
            PolylineEntity => "open polyline",
            BezierSplineEntity { IsClosed: true } => "closed spline",
            BezierSplineEntity => "open spline",
            TextEntity => "text",
            MultilineTextEntity => "multiline text",
            DimensionEntity => "dimension",
            PointEntity => "point",
            _ => "entity"
        };
    }
}
