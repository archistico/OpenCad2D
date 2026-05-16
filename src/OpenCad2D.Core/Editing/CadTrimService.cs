using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides trim operations for the editable entity types supported by OpenCad2D.
/// </summary>
public static class CadTrimService
{
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

        if (target is LineEntity line)
        {
            return TrimLineByBoundaries(
                line,
                boundaries,
                targetPickPoint,
                effectiveTolerance);
        }

        if (target is EllipseEntity ellipse)
        {
            return TrimEllipseByBoundaries(
                ellipse,
                boundaries,
                targetPickPoint,
                effectiveTolerance);
        }

        if (target is PolylineEntity polyline)
        {
            return TrimPolylineByBoundaries(
                polyline,
                boundaries,
                targetPickPoint,
                effectiveTolerance);
        }

        if (boundaries.Count > 1)
        {
            return Array.Empty<CadEntity>();
        }

        CadEntity boundary = boundaries[0];

        return target switch
        {
            CircleEntity circle => TrimCircle(circle, boundary, targetPickPoint, effectiveTolerance),
            ArcEntity arc => TrimArc(arc, boundary, targetPickPoint, effectiveTolerance),
            _ => Array.Empty<CadEntity>()
        };
    }

    private static IReadOnlyList<CadEntity> TrimLineByBoundaries(
        LineEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        var cuts = new List<PathCut>
        {
            new(0.0, target.Start),
            new(1.0, target.End)
        };

        foreach (CadEntity boundary in boundaries)
        {
            foreach (Point2D point in CadEntityIntersectionService
                         .Intersect(target, boundary, tolerance)
                         .Where(point => !tolerance.ArePointsEqual(point, target.Start) &&
                                         !tolerance.ArePointsEqual(point, target.End)))
            {
                double parameter = LineParameterService.GetParameter(
                    target.Geometry,
                    point,
                    tolerance);

                if (parameter > tolerance.Parameter &&
                    parameter < 1.0 - tolerance.Parameter)
                {
                    cuts.Add(new PathCut(parameter, point));
                }
            }
        }

        if (cuts.Count <= 2)
        {
            return Array.Empty<CadEntity>();
        }

        return CreateLineFragments(
            target,
            cuts,
            LineParameterService.GetParameter(
                target.Geometry,
                pickPoint,
                tolerance),
            tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimLine(
        LineEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CadEntityIntersectionService
            .Intersect(target, boundary, tolerance)
            .Where(point => !tolerance.ArePointsEqual(point, target.Start) &&
                            !tolerance.ArePointsEqual(point, target.End))
            .ToList();

        if (intersections.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        var cuts = new List<PathCut>
        {
            new(0.0, target.Start),
            new(1.0, target.End)
        };

        foreach (Point2D point in intersections)
        {
            double parameter = LineParameterService.GetParameter(
                target.Geometry,
                point,
                tolerance);

            if (parameter > tolerance.Parameter && parameter < 1.0 - tolerance.Parameter)
            {
                cuts.Add(new PathCut(parameter, point));
            }
        }

        return CreateLineFragments(target, cuts, LineParameterService.GetParameter(
            target.Geometry,
            pickPoint,
            tolerance), tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimCircle(
        CircleEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CadEntityIntersectionService
            .Intersect(target, boundary, tolerance)
            .ToList();

        if (intersections.Count < 2)
        {
            return Array.Empty<CadEntity>();
        }

        List<PathCut> cuts = intersections
            .Select(point => new PathCut(
                NormalizeRadians(Math.Atan2(
                    point.Y - target.Center.Y,
                    point.X - target.Center.X)),
                point))
            .ToList();

        double pickAngle = NormalizeRadians(Math.Atan2(
            pickPoint.Y - target.Center.Y,
            pickPoint.X - target.Center.X));

        return CreateCircleArcFragments(target, cuts, pickAngle, tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimArc(
        ArcEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> intersections = CadEntityIntersectionService
            .Intersect(target, boundary, tolerance)
            .Where(point => !tolerance.ArePointsEqual(point, target.Geometry.StartPoint) &&
                            !tolerance.ArePointsEqual(point, target.Geometry.EndPoint))
            .ToList();

        if (intersections.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        List<PathCut> cuts = new()
        {
            new(0.0, target.Geometry.StartPoint),
            new(GetArcSweep(target.Geometry), target.Geometry.EndPoint)
        };

        foreach (Point2D point in intersections)
        {
            double parameter = GetDistanceAlongArc(target.Geometry, point);

            if (parameter > tolerance.Parameter &&
                parameter < GetArcSweep(target.Geometry) - tolerance.Parameter)
            {
                cuts.Add(new PathCut(parameter, point));
            }
        }

        double pickParameter = GetDistanceAlongArc(target.Geometry, pickPoint);

        return CreateArcFragments(target, cuts, pickParameter, tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimEllipseByBoundaries(
        EllipseEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        List<PathCut> cuts = new();

        foreach (CadEntity boundary in boundaries)
        {
            foreach (Point2D point in CadEntityIntersectionService.Intersect(
                         target,
                         boundary,
                         tolerance))
            {
                cuts.Add(new PathCut(
                    GetEllipseParameter(target, point),
                    point));
            }
        }

        if (cuts.Count < 2)
        {
            return Array.Empty<CadEntity>();
        }

        double pickParameter = GetEllipseParameter(
            target,
            target.GetClosestPoint(pickPoint));

        return CreateEllipsePolylineFragments(target, cuts, pickParameter, tolerance);
    }

    private static IReadOnlyList<CadEntity> TrimPolylineByBoundaries(
        PolylineEntity target,
        IReadOnlyList<CadEntity> boundaries,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<LineSegment2D> segments = target.Geometry.GetSegments();
        if (segments.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        var cuts = new List<PathCut>
        {
            new(0.0, segments[0].Start)
        };

        double accumulated = 0.0;
        double pickParameter = GetPolylinePathParameter(target, pickPoint, tolerance);
        int intersectionCutCount = 0;

        for (int index = 0; index < segments.Count; index++)
        {
            LineSegment2D segment = segments[index];
            var segmentEntity = new LineEntity(segment.Start, segment.End);

            foreach (CadEntity boundary in boundaries)
            {
                foreach (Point2D point in CadEntityIntersectionService.Intersect(
                             segmentEntity,
                             boundary,
                             tolerance))
                {
                    double localParameter = LineParameterService.GetParameter(
                        segment,
                        point,
                        tolerance);

                    if (localParameter > tolerance.Parameter &&
                        localParameter < 1.0 - tolerance.Parameter)
                    {
                        cuts.Add(new PathCut(
                            accumulated + localParameter * segment.Length,
                            point));
                        intersectionCutCount++;
                    }
                }
            }

            accumulated += segment.Length;
            cuts.Add(new PathCut(accumulated, segment.End));
        }

        if (intersectionCutCount == 0)
        {
            return Array.Empty<CadEntity>();
        }

        return CreatePolylineFragments(target, cuts, pickParameter, tolerance);
    }

    private static IReadOnlyList<CadEntity> CreateLineFragments(
        LineEntity source,
        List<PathCut> cuts,
        double pickParameter,
        GeometryTolerance tolerance)
    {
        var normalizedCuts = NormalizeCuts(cuts, tolerance);
        int intervalToRemove = FindIntervalContaining(normalizedCuts, pickParameter);
        var keptIntervals = new List<(Point2D Start, Point2D End)>();

        for (int index = 0; index < normalizedCuts.Count - 1; index++)
        {
            if (index == intervalToRemove)
            {
                continue;
            }

            Point2D start = normalizedCuts[index].Point;
            Point2D end = normalizedCuts[index + 1].Point;

            if (start.DistanceTo(end) <= tolerance.Distance)
            {
                continue;
            }

            keptIntervals.Add((start, end));
        }

        return CreateMergedLineFragments(
            source,
            keptIntervals,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> CreateMergedLineFragments(
        LineEntity source,
        IReadOnlyList<(Point2D Start, Point2D End)> intervals,
        GeometryTolerance tolerance)
    {
        var result = new List<CadEntity>();

        foreach ((Point2D start, Point2D end) in intervals)
        {
            if (start.DistanceTo(end) <= tolerance.Distance)
            {
                continue;
            }

            if (result.LastOrDefault() is LineEntity previous &&
                tolerance.ArePointsEqual(previous.End, start))
            {
                result[^1] = new LineEntity(
                    previous.Start,
                    end,
                    layerId: source.LayerId,
                    style: source.Style,
                    isVisible: source.IsVisible,
                    isLocked: source.IsLocked,
                    drawOrder: source.DrawOrder);
                continue;
            }

            result.Add(new LineEntity(
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

    private static IReadOnlyList<CadEntity> CreateCircleArcFragments(
        CircleEntity source,
        List<PathCut> cuts,
        double pickAngle,
        GeometryTolerance tolerance)
    {
        var normalizedCuts = NormalizeCuts(cuts, tolerance);
        var result = new List<CadEntity>();

        for (int index = 0; index < normalizedCuts.Count; index++)
        {
            PathCut start = normalizedCuts[index];
            PathCut end = normalizedCuts[(index + 1) % normalizedCuts.Count];
            double startValue = start.Parameter;
            double endValue = end.Parameter;

            if (endValue <= startValue)
            {
                endValue += TwoPi;
            }

            double normalizedPick = pickAngle;
            if (normalizedPick < startValue)
            {
                normalizedPick += TwoPi;
            }

            if (normalizedPick >= startValue - tolerance.Angle &&
                normalizedPick <= endValue + tolerance.Angle)
            {
                continue;
            }

            if (Math.Abs(endValue - startValue) <= tolerance.Angle)
            {
                continue;
            }

            result.Add(new ArcEntity(
                source.Center,
                source.Radius,
                Angle.FromRadians(start.Parameter),
                Angle.FromRadians(end.Parameter),
                isCounterClockwise: true,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder));
        }

        return result;
    }

    private static IReadOnlyList<CadEntity> CreateArcFragments(
        ArcEntity source,
        List<PathCut> cuts,
        double pickParameter,
        GeometryTolerance tolerance)
    {
        var normalizedCuts = NormalizeCuts(cuts, tolerance);
        int intervalToRemove = FindIntervalContaining(normalizedCuts, pickParameter);
        var result = new List<CadEntity>();

        for (int index = 0; index < normalizedCuts.Count - 1; index++)
        {
            if (index == intervalToRemove)
            {
                continue;
            }

            Point2D start = normalizedCuts[index].Point;
            Point2D end = normalizedCuts[index + 1].Point;

            if (start.DistanceTo(end) <= tolerance.Distance)
            {
                continue;
            }

            result.Add(new ArcEntity(
                source.Center,
                source.Radius,
                GetAngle(source.Center, start),
                GetAngle(source.Center, end),
                source.IsCounterClockwise,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder));
        }

        return result;
    }

    private static IReadOnlyList<CadEntity> CreateEllipsePolylineFragments(
        EllipseEntity source,
        List<PathCut> cuts,
        double pickParameter,
        GeometryTolerance tolerance)
    {
        var normalizedCuts = NormalizeCuts(cuts, tolerance);
        var result = new List<CadEntity>();

        for (int index = 0; index < normalizedCuts.Count; index++)
        {
            PathCut start = normalizedCuts[index];
            PathCut end = normalizedCuts[(index + 1) % normalizedCuts.Count];
            double startValue = start.Parameter;
            double endValue = end.Parameter;

            if (endValue <= startValue)
            {
                endValue += TwoPi;
            }

            double normalizedPick = pickParameter;
            if (normalizedPick < startValue)
            {
                normalizedPick += TwoPi;
            }

            if (normalizedPick >= startValue - tolerance.Angle &&
                normalizedPick <= endValue + tolerance.Angle)
            {
                continue;
            }

            AddEllipsePolylineFragmentIfValid(
                result,
                source,
                startValue,
                endValue,
                tolerance);
        }

        return result;
    }

    private static void AddEllipsePolylineFragmentIfValid(
        ICollection<CadEntity> result,
        EllipseEntity source,
        double startParameter,
        double endParameter,
        GeometryTolerance tolerance)
    {
        if (endParameter - startParameter <= tolerance.Angle)
        {
            return;
        }

        result.Add(new PolylineEntity(
            CreateEllipsePolylineVertices(source, startParameter, endParameter),
            isClosed: false,
            layerId: source.LayerId,
            style: source.Style,
            isVisible: source.IsVisible,
            isLocked: source.IsLocked,
            drawOrder: source.DrawOrder));
    }

    private static IReadOnlyList<Point2D> CreateEllipsePolylineVertices(
        EllipseEntity source,
        double startParameter,
        double endParameter)
    {
        double sweep = endParameter - startParameter;
        int segmentCount = Math.Max(
            2,
            (int)Math.Ceiling(EllipseEntity.DefaultSampleCount * sweep / TwoPi));

        var vertices = new List<Point2D>(segmentCount + 1);
        for (int index = 0; index <= segmentCount; index++)
        {
            double parameter = startParameter + sweep * index / segmentCount;
            vertices.Add(source.GetPointAt(parameter));
        }

        return vertices;
    }

    private static IReadOnlyList<CadEntity> CreatePolylineFragments(
        PolylineEntity source,
        List<PathCut> cuts,
        double pickParameter,
        GeometryTolerance tolerance)
    {
        var normalizedCuts = NormalizeCuts(cuts, tolerance);
        int intervalToRemove = FindIntervalContaining(normalizedCuts, pickParameter);
        var result = new List<CadEntity>();

        for (int index = 0; index < normalizedCuts.Count - 1; index++)
        {
            if (index == intervalToRemove)
            {
                continue;
            }

            Point2D start = normalizedCuts[index].Point;
            Point2D end = normalizedCuts[index + 1].Point;

            if (start.DistanceTo(end) <= tolerance.Distance)
            {
                continue;
            }

            result.Add(new PolylineEntity(
                new[] { start, end },
                isClosed: false,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder));
        }

        return result;
    }

    private static double GetPolylinePathParameter(
        PolylineEntity polyline,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        double accumulated = 0.0;
        double bestDistance = double.MaxValue;
        double bestParameter = 0.0;

        foreach (LineSegment2D segment in polyline.Geometry.GetSegments())
        {
            Point2D closest = OpenCad2D.Geometry.Operations.DistanceService.ClosestPointOnSegment(
                pickPoint,
                segment);
            double distance = pickPoint.DistanceTo(closest);
            double local = LineParameterService.GetParameter(segment, closest, tolerance);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestParameter = accumulated + Math.Clamp(local, 0.0, 1.0) * segment.Length;
            }

            accumulated += segment.Length;
        }

        return bestParameter;
    }

    private static List<PathCut> NormalizeCuts(
        IEnumerable<PathCut> cuts,
        GeometryTolerance tolerance)
    {
        var sorted = cuts
            .OrderBy(cut => cut.Parameter)
            .ToList();

        var result = new List<PathCut>();

        foreach (PathCut cut in sorted)
        {
            if (result.Count > 0 &&
                Math.Abs(result[^1].Parameter - cut.Parameter) <= tolerance.Parameter)
            {
                continue;
            }

            result.Add(cut);
        }

        return result;
    }

    private static int FindIntervalContaining(
        IReadOnlyList<PathCut> cuts,
        double parameter)
    {
        for (int index = 0; index < cuts.Count - 1; index++)
        {
            if (parameter >= cuts[index].Parameter &&
                parameter <= cuts[index + 1].Parameter)
            {
                return index;
            }
        }

        return parameter < cuts[0].Parameter
            ? 0
            : cuts.Count - 2;
    }

    private static double GetArcSweep(Arc2D arc)
    {
        double start = NormalizeRadians(arc.StartAngle.Radians);
        double end = NormalizeRadians(arc.EndAngle.Radians);

        if (arc.IsCounterClockwise)
        {
            return end >= start
                ? end - start
                : end + TwoPi - start;
        }

        return start >= end
            ? start - end
            : start + TwoPi - end;
    }

    private static double GetDistanceAlongArc(Arc2D arc, Point2D point)
    {
        double start = NormalizeRadians(arc.StartAngle.Radians);
        double value = NormalizeRadians(Math.Atan2(
            point.Y - arc.Center.Y,
            point.X - arc.Center.X));

        if (arc.IsCounterClockwise)
        {
            return value >= start
                ? value - start
                : value + TwoPi - start;
        }

        return start >= value
            ? start - value
            : start + TwoPi - value;
    }

    private static double GetEllipseParameter(EllipseEntity ellipse, Point2D point)
    {
        Vector2D fromCenter = ellipse.Center.VectorTo(point);
        Vector2D majorDirection = ellipse.MajorDirection;
        Vector2D minorDirection = ellipse.MinorAxis.Normalize();

        double localX = fromCenter.Dot(majorDirection) / ellipse.MajorRadius;
        double localY = fromCenter.Dot(minorDirection) / ellipse.MinorRadius;

        return NormalizeRadians(Math.Atan2(localY, localX));
    }

    private static Angle GetAngle(Point2D center, Point2D point)
    {
        return Angle.FromRadians(Math.Atan2(
            point.Y - center.Y,
            point.X - center.X));
    }

    private static double NormalizeRadians(double radians)
    {
        double value = radians % TwoPi;
        return value < 0 ? value + TwoPi : value;
    }

    private const double TwoPi = Math.PI * 2.0;

    private readonly record struct PathCut(double Parameter, Point2D Point);
}
