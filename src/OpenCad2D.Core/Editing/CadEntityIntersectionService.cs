using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Editing.Curves;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides intersection helpers for editable CAD entities.
/// </summary>
public static class CadEntityIntersectionService
{

    /// <summary>
    /// Finds shared intersection points together with native curve parameters for both entities.
    /// </summary>
    /// <remarks>
    /// This richer result is intended for CAD editing commands that must reuse the same geometric
    /// point across entities while rebuilding each entity from its own native parameter.
    /// </remarks>
    public static IReadOnlyList<CadIntersectionPoint> IntersectDetailed(
        CadEntity first,
        CadEntity second,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        var adapterFactory = new DefaultCurveAdapterFactory();

        if (!adapterFactory.TryCreate(first, out ICurveAdapter firstAdapter) ||
            !adapterFactory.TryCreate(second, out ICurveAdapter secondAdapter))
        {
            return Array.Empty<CadIntersectionPoint>();
        }

        IReadOnlyList<Point2D> points = Intersect(first, second, effectiveTolerance);
        var result = new List<CadIntersectionPoint>();

        foreach (Point2D point in points)
        {
            if (!firstAdapter.TryProjectPointToCut(point, effectiveTolerance, out CurveCut firstCut) ||
                !secondAdapter.TryProjectPointToCut(point, effectiveTolerance, out CurveCut secondCut))
            {
                continue;
            }

            AddDistinct(
                result,
                new CadIntersectionPoint(
                    point,
                    firstCut.Parameter,
                    secondCut.Parameter,
                    ClassifyIntersection(firstAdapter, secondAdapter, firstCut, secondCut, effectiveTolerance)),
                effectiveTolerance.Distance);
        }

        return result;
    }

    public static IReadOnlyList<Point2D> Intersect(
        CadEntity first,
        CadEntity second,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        double distanceTolerance = effectiveTolerance.Distance;

        if (first is LineEntity firstLine && second is EllipseEntity secondEllipse)
        {
            return IntersectSegmentEllipse(
                firstLine.Geometry,
                secondEllipse.Center,
                secondEllipse.MajorDirection,
                secondEllipse.MajorRadius,
                secondEllipse.MinorAxis.Normalize(),
                secondEllipse.MinorRadius,
                effectiveTolerance);
        }

        if (first is EllipseEntity firstEllipse && second is LineEntity secondLine)
        {
            return IntersectSegmentEllipse(
                secondLine.Geometry,
                firstEllipse.Center,
                firstEllipse.MajorDirection,
                firstEllipse.MajorRadius,
                firstEllipse.MinorAxis.Normalize(),
                firstEllipse.MinorRadius,
                effectiveTolerance);
        }

        if (first is LineEntity firstLineForArc && second is EllipticalArcEntity secondEllipticalArc)
        {
            return IntersectSegmentEllipticalArc(
                firstLineForArc.Geometry,
                secondEllipticalArc,
                effectiveTolerance);
        }

        if (first is EllipticalArcEntity firstEllipticalArc && second is LineEntity secondLineForArc)
        {
            return IntersectSegmentEllipticalArc(
                secondLineForArc.Geometry,
                firstEllipticalArc,
                effectiveTolerance);
        }

        if (first is PolylineEntity firstPolyline && second is EllipseEntity secondEllipseForPolyline)
        {
            return IntersectPolylineEllipse(
                firstPolyline,
                secondEllipseForPolyline,
                effectiveTolerance);
        }

        if (first is EllipseEntity firstEllipseForPolyline && second is PolylineEntity secondPolyline)
        {
            return IntersectPolylineEllipse(
                secondPolyline,
                firstEllipseForPolyline,
                effectiveTolerance);
        }

        if (first is PolylineEntity firstPolylineForArc && second is EllipticalArcEntity secondEllipticalArcForPolyline)
        {
            return IntersectPolylineEllipticalArc(
                firstPolylineForArc,
                secondEllipticalArcForPolyline,
                effectiveTolerance);
        }

        if (first is EllipticalArcEntity firstEllipticalArcForPolyline && second is PolylineEntity secondPolylineForArc)
        {
            return IntersectPolylineEllipticalArc(
                secondPolylineForArc,
                firstEllipticalArcForPolyline,
                effectiveTolerance);
        }

        if (first is CircleEntity firstCircle && second is EllipseEntity secondEllipseForCircle)
        {
            return IntersectCircleEllipse(
                firstCircle.Geometry,
                secondEllipseForCircle,
                effectiveTolerance);
        }

        if (first is EllipseEntity firstEllipseForCircle && second is CircleEntity secondCircle)
        {
            return IntersectCircleEllipse(
                secondCircle.Geometry,
                firstEllipseForCircle,
                effectiveTolerance);
        }

        if (first is CircleEntity firstCircleForEllipticalArc && second is EllipticalArcEntity secondEllipticalArcForCircle)
        {
            return IntersectCircleEllipticalArc(
                firstCircleForEllipticalArc.Geometry,
                secondEllipticalArcForCircle,
                effectiveTolerance);
        }

        if (first is EllipticalArcEntity firstEllipticalArcForCircle && second is CircleEntity secondCircleForEllipticalArc)
        {
            return IntersectCircleEllipticalArc(
                secondCircleForEllipticalArc.Geometry,
                firstEllipticalArcForCircle,
                effectiveTolerance);
        }

        if (first is ArcEntity firstArcForEllipse && second is EllipseEntity secondEllipseForArc)
        {
            return IntersectArcEllipse(
                firstArcForEllipse.Geometry,
                secondEllipseForArc,
                effectiveTolerance);
        }

        if (first is EllipseEntity firstEllipseForArc && second is ArcEntity secondArcForEllipse)
        {
            return IntersectArcEllipse(
                secondArcForEllipse.Geometry,
                firstEllipseForArc,
                effectiveTolerance);
        }

        if (first is ArcEntity firstArcForEllipticalArc && second is EllipticalArcEntity secondEllipticalArcForCircularArc)
        {
            return IntersectArcEllipticalArc(
                firstArcForEllipticalArc.Geometry,
                secondEllipticalArcForCircularArc,
                effectiveTolerance);
        }

        if (first is EllipticalArcEntity firstEllipticalArcForCircularArc && second is ArcEntity secondArcForEllipticalArc)
        {
            return IntersectArcEllipticalArc(
                secondArcForEllipticalArc.Geometry,
                firstEllipticalArcForCircularArc,
                effectiveTolerance);
        }

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

        if (entity is EllipseEntity ellipse)
        {
            return IntersectInfiniteLineEllipse(
                infiniteLine,
                ellipse.Center,
                ellipse.MajorDirection,
                ellipse.MajorRadius,
                ellipse.MinorAxis.Normalize(),
                ellipse.MinorRadius,
                effectiveTolerance);
        }

        if (entity is EllipticalArcEntity ellipticalArc)
        {
            return IntersectInfiniteLineEllipse(
                    infiniteLine,
                    ellipticalArc.Center,
                    ellipticalArc.MajorDirection,
                    ellipticalArc.MajorRadius,
                    ellipticalArc.MinorAxis.Normalize(),
                    ellipticalArc.MinorRadius,
                    effectiveTolerance)
                .Where(point => IsPointOnEllipticalArc(point, ellipticalArc, effectiveTolerance))
                .ToList();
        }

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

        if (entity is EllipseEntity ellipse)
        {
            return IntersectCircleEllipse(circle, ellipse, effectiveTolerance);
        }

        if (entity is EllipticalArcEntity ellipticalArc)
        {
            return IntersectCircleEllipticalArc(circle, ellipticalArc, effectiveTolerance);
        }

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


    private static IReadOnlyList<Point2D> IntersectPolylineEllipse(
        PolylineEntity polyline,
        EllipseEntity ellipse,
        GeometryTolerance tolerance)
    {
        var result = new List<Point2D>();

        foreach (LineSegment2D segment in polyline.Geometry.GetSegments())
        {
            foreach (Point2D point in IntersectSegmentEllipse(
                         segment,
                         ellipse.Center,
                         ellipse.MajorDirection,
                         ellipse.MajorRadius,
                         ellipse.MinorAxis.Normalize(),
                         ellipse.MinorRadius,
                         tolerance))
            {
                AddDistinct(result, point, tolerance.Distance);
            }
        }

        return result;
    }

    private static IReadOnlyList<Point2D> IntersectPolylineEllipticalArc(
        PolylineEntity polyline,
        EllipticalArcEntity arc,
        GeometryTolerance tolerance)
    {
        var result = new List<Point2D>();

        foreach (LineSegment2D segment in polyline.Geometry.GetSegments())
        {
            foreach (Point2D point in IntersectSegmentEllipticalArc(
                         segment,
                         arc,
                         tolerance))
            {
                AddDistinct(result, point, tolerance.Distance);
            }
        }

        return result;
    }

    private static IReadOnlyList<Point2D> IntersectSegmentEllipticalArc(
        LineSegment2D segment,
        EllipticalArcEntity arc,
        GeometryTolerance tolerance)
    {
        return IntersectSegmentEllipse(
                segment,
                arc.Center,
                arc.MajorDirection,
                arc.MajorRadius,
                arc.MinorAxis.Normalize(),
                arc.MinorRadius,
                tolerance)
            .Where(point => IsPointOnEllipticalArc(point, arc, tolerance))
            .ToList();
    }

    private static IReadOnlyList<Point2D> IntersectCircleEllipse(
        Circle2D circle,
        EllipseEntity ellipse,
        GeometryTolerance tolerance)
    {
        return IntersectCircleEllipseParameters(
            circle,
            ellipse.Center,
            ellipse.MajorAxis,
            ellipse.MinorAxis,
            0.0,
            Math.Tau,
            tolerance,
            point => true);
    }

    private static IReadOnlyList<Point2D> IntersectCircleEllipticalArc(
        Circle2D circle,
        EllipticalArcEntity arc,
        GeometryTolerance tolerance)
    {
        return IntersectCircleEllipseParameters(
            circle,
            arc.Center,
            arc.MajorAxis,
            arc.MinorAxis,
            arc.StartParameterRadians,
            arc.IsCounterClockwise
                ? arc.StartParameterRadians + arc.SweepRadians
                : arc.StartParameterRadians - arc.SweepRadians,
            tolerance,
            point => IsPointOnEllipticalArc(point, arc, tolerance));
    }

    private static IReadOnlyList<Point2D> IntersectArcEllipse(
        Arc2D arc,
        EllipseEntity ellipse,
        GeometryTolerance tolerance)
    {
        return IntersectCircleEllipse(
                new Circle2D(arc.Center, arc.Radius),
                ellipse,
                tolerance)
            .Where(point => arc.ContainsPoint(point, tolerance.Distance))
            .ToList();
    }

    private static IReadOnlyList<Point2D> IntersectArcEllipticalArc(
        Arc2D circularArc,
        EllipticalArcEntity ellipticalArc,
        GeometryTolerance tolerance)
    {
        return IntersectCircleEllipticalArc(
                new Circle2D(circularArc.Center, circularArc.Radius),
                ellipticalArc,
                tolerance)
            .Where(point => circularArc.ContainsPoint(point, tolerance.Distance))
            .ToList();
    }

    private static IReadOnlyList<Point2D> IntersectCircleEllipseParameters(
        Circle2D circle,
        Point2D ellipseCenter,
        Vector2D majorAxis,
        Vector2D minorAxis,
        double startParameter,
        double endParameter,
        GeometryTolerance tolerance,
        Func<Point2D, bool> pointFilter)
    {
        double start = startParameter;
        double end = endParameter;
        if (end < start)
        {
            (start, end) = (end, start);
        }

        double span = end - start;
        if (span <= tolerance.Angle)
        {
            return Array.Empty<Point2D>();
        }

        var result = new List<Point2D>();
        const int sampleCount = 1440;
        double step = span / sampleCount;
        double previousParameter = start;
        double previousValue = EvaluateEllipseCircleDistanceEquation(
            ellipseCenter,
            majorAxis,
            minorAxis,
            circle,
            previousParameter);

        AddEllipseCircleRootIfValid(
            result,
            circle,
            ellipseCenter,
            majorAxis,
            minorAxis,
            previousParameter,
            tolerance,
            pointFilter);

        for (int index = 1; index <= sampleCount; index++)
        {
            double currentParameter = index == sampleCount
                ? end
                : start + step * index;
            double currentValue = EvaluateEllipseCircleDistanceEquation(
                ellipseCenter,
                majorAxis,
                minorAxis,
                circle,
                currentParameter);

            if (HasSignChange(previousValue, currentValue))
            {
                double root = BisectRoot(
                    parameter => EvaluateEllipseCircleDistanceEquation(
                        ellipseCenter,
                        majorAxis,
                        minorAxis,
                        circle,
                        parameter),
                    previousParameter,
                    currentParameter,
                    tolerance.Angle);

                AddEllipseCircleRootIfValid(
                    result,
                    circle,
                    ellipseCenter,
                    majorAxis,
                    minorAxis,
                    root,
                    tolerance,
                    pointFilter);
            }

            if (HasSignChange(
                    EvaluateEllipseCircleDerivativeEquation(
                        ellipseCenter,
                        majorAxis,
                        minorAxis,
                        circle,
                        previousParameter),
                    EvaluateEllipseCircleDerivativeEquation(
                        ellipseCenter,
                        majorAxis,
                        minorAxis,
                        circle,
                        currentParameter)))
            {
                double stationary = BisectRoot(
                    parameter => EvaluateEllipseCircleDerivativeEquation(
                        ellipseCenter,
                        majorAxis,
                        minorAxis,
                        circle,
                        parameter),
                    previousParameter,
                    currentParameter,
                    tolerance.Angle);

                AddEllipseCircleRootIfValid(
                    result,
                    circle,
                    ellipseCenter,
                    majorAxis,
                    minorAxis,
                    stationary,
                    tolerance,
                    pointFilter);
            }

            AddEllipseCircleRootIfValid(
                result,
                circle,
                ellipseCenter,
                majorAxis,
                minorAxis,
                currentParameter,
                tolerance,
                pointFilter);

            previousParameter = currentParameter;
            previousValue = currentValue;
        }

        return result;
    }

    private static bool HasSignChange(double first, double second)
    {
        return (first < 0.0 && second > 0.0) ||
               (first > 0.0 && second < 0.0);
    }

    private static double BisectRoot(
        Func<double, double> function,
        double start,
        double end,
        double parameterTolerance)
    {
        double low = start;
        double high = end;
        double lowValue = function(low);

        for (int iteration = 0; iteration < 80; iteration++)
        {
            double mid = (low + high) / 2.0;
            double midValue = function(mid);

            if (Math.Abs(high - low) <= parameterTolerance)
            {
                return mid;
            }

            if (HasSignChange(lowValue, midValue))
            {
                high = mid;
                continue;
            }

            low = mid;
            lowValue = midValue;
        }

        return (low + high) / 2.0;
    }

    private static double EvaluateEllipseCircleDistanceEquation(
        Point2D ellipseCenter,
        Vector2D majorAxis,
        Vector2D minorAxis,
        Circle2D circle,
        double parameter)
    {
        Point2D point = GetEllipsePointAt(
            ellipseCenter,
            majorAxis,
            minorAxis,
            parameter);

        double distance = point.DistanceTo(circle.Center);
        return distance * distance - circle.Radius * circle.Radius;
    }

    private static double EvaluateEllipseCircleDerivativeEquation(
        Point2D ellipseCenter,
        Vector2D majorAxis,
        Vector2D minorAxis,
        Circle2D circle,
        double parameter)
    {
        Point2D point = GetEllipsePointAt(
            ellipseCenter,
            majorAxis,
            minorAxis,
            parameter);
        Vector2D derivative = (majorAxis * -Math.Sin(parameter)) +
                              (minorAxis * Math.Cos(parameter));

        return 2.0 * circle.Center.VectorTo(point).Dot(derivative);
    }

    private static void AddEllipseCircleRootIfValid(
        List<Point2D> result,
        Circle2D circle,
        Point2D ellipseCenter,
        Vector2D majorAxis,
        Vector2D minorAxis,
        double parameter,
        GeometryTolerance tolerance,
        Func<Point2D, bool> pointFilter)
    {
        Point2D point = GetEllipsePointAt(
            ellipseCenter,
            majorAxis,
            minorAxis,
            parameter);

        double circleResidual = Math.Abs(point.DistanceTo(circle.Center) - circle.Radius);
        double acceptanceTolerance = Math.Max(
            tolerance.Distance * 100.0,
            tolerance.Distance);

        if (circleResidual > acceptanceTolerance || !pointFilter(point))
        {
            return;
        }

        AddDistinct(result, point, tolerance.Distance);
    }

    private static Point2D GetEllipsePointAt(
        Point2D center,
        Vector2D majorAxis,
        Vector2D minorAxis,
        double parameter)
    {
        return center +
               majorAxis * Math.Cos(parameter) +
               minorAxis * Math.Sin(parameter);
    }

    private static IReadOnlyList<Point2D> IntersectInfiniteLineEllipse(
        Line2D line,
        Point2D center,
        Vector2D majorDirection,
        double majorRadius,
        Vector2D minorDirection,
        double minorRadius,
        GeometryTolerance tolerance)
    {
        Vector2D fromCenter = center.VectorTo(line.Point);
        Vector2D direction = line.Direction;

        double startX = fromCenter.Dot(majorDirection) / majorRadius;
        double startY = fromCenter.Dot(minorDirection) / minorRadius;
        double directionX = direction.Dot(majorDirection) / majorRadius;
        double directionY = direction.Dot(minorDirection) / minorRadius;

        double a = directionX * directionX + directionY * directionY;
        double b = 2.0 * (startX * directionX + startY * directionY);
        double c = startX * startX + startY * startY - 1.0;

        if (Math.Abs(a) <= tolerance.Parameter)
        {
            return Array.Empty<Point2D>();
        }

        double discriminant = b * b - 4.0 * a * c;
        if (discriminant < -tolerance.Distance)
        {
            return Array.Empty<Point2D>();
        }

        var result = new List<Point2D>();
        if (Math.Abs(discriminant) <= tolerance.Distance)
        {
            AddInfiniteLineEllipseIntersection(result, line, -b / (2.0 * a), tolerance);
            return result;
        }

        double sqrtDiscriminant = Math.Sqrt(Math.Max(0.0, discriminant));
        AddInfiniteLineEllipseIntersection(result, line, (-b - sqrtDiscriminant) / (2.0 * a), tolerance);
        AddInfiniteLineEllipseIntersection(result, line, (-b + sqrtDiscriminant) / (2.0 * a), tolerance);
        return result;
    }

    private static void AddInfiniteLineEllipseIntersection(
        List<Point2D> result,
        Line2D line,
        double parameter,
        GeometryTolerance tolerance)
    {
        Point2D point = new(
            line.Point.X + line.Direction.X * parameter,
            line.Point.Y + line.Direction.Y * parameter);

        AddDistinct(result, point, tolerance.Distance);
    }

    private static IReadOnlyList<Point2D> IntersectSegmentEllipse(
        LineSegment2D segment,
        Point2D center,
        Vector2D majorDirection,
        double majorRadius,
        Vector2D minorDirection,
        double minorRadius,
        GeometryTolerance tolerance)
    {
        Vector2D fromCenter = center.VectorTo(segment.Start);
        Vector2D direction = segment.Start.VectorTo(segment.End);

        double startX = fromCenter.Dot(majorDirection) / majorRadius;
        double startY = fromCenter.Dot(minorDirection) / minorRadius;
        double directionX = direction.Dot(majorDirection) / majorRadius;
        double directionY = direction.Dot(minorDirection) / minorRadius;

        double a = directionX * directionX + directionY * directionY;
        double b = 2.0 * (startX * directionX + startY * directionY);
        double c = startX * startX + startY * startY - 1.0;

        if (Math.Abs(a) <= tolerance.Parameter)
        {
            return Array.Empty<Point2D>();
        }

        double discriminant = b * b - 4.0 * a * c;
        if (discriminant < -tolerance.Distance)
        {
            return Array.Empty<Point2D>();
        }

        var result = new List<Point2D>();
        if (Math.Abs(discriminant) <= tolerance.Distance)
        {
            AddSegmentEllipseIntersection(result, segment, direction, -b / (2.0 * a), tolerance);
            return result;
        }

        double sqrtDiscriminant = Math.Sqrt(Math.Max(0.0, discriminant));
        AddSegmentEllipseIntersection(result, segment, direction, (-b - sqrtDiscriminant) / (2.0 * a), tolerance);
        AddSegmentEllipseIntersection(result, segment, direction, (-b + sqrtDiscriminant) / (2.0 * a), tolerance);
        return result;
    }

    private static void AddSegmentEllipseIntersection(
        List<Point2D> result,
        LineSegment2D segment,
        Vector2D direction,
        double parameter,
        GeometryTolerance tolerance)
    {
        if (parameter < -tolerance.Parameter ||
            parameter > 1.0 + tolerance.Parameter)
        {
            return;
        }

        double clamped = Math.Clamp(parameter, 0.0, 1.0);
        Point2D point = new(
            segment.Start.X + direction.X * clamped,
            segment.Start.Y + direction.Y * clamped);

        AddDistinct(result, point, tolerance.Distance);
    }

    private static bool IsPointOnEllipticalArc(
        Point2D point,
        EllipticalArcEntity arc,
        GeometryTolerance tolerance)
    {
        double parameter = GetEllipseParameter(
            arc.Center,
            arc.MajorDirection,
            arc.MajorRadius,
            arc.MinorAxis.Normalize(),
            arc.MinorRadius,
            point);

        double normalized = GetDirectedParameterDistance(
            arc.StartParameterRadians,
            parameter,
            arc.IsCounterClockwise);

        return normalized >= -tolerance.Angle &&
               normalized <= arc.SweepRadians + tolerance.Angle;
    }

    private static double GetEllipseParameter(
        Point2D center,
        Vector2D majorDirection,
        double majorRadius,
        Vector2D minorDirection,
        double minorRadius,
        Point2D point)
    {
        Vector2D fromCenter = center.VectorTo(point);
        double localX = fromCenter.Dot(majorDirection) / majorRadius;
        double localY = fromCenter.Dot(minorDirection) / minorRadius;

        return NormalizeRadians(Math.Atan2(localY, localX));
    }

    private static double GetDirectedParameterDistance(
        double startParameter,
        double endParameter,
        bool isCounterClockwise)
    {
        double start = NormalizeRadians(startParameter);
        double end = NormalizeRadians(endParameter);

        if (isCounterClockwise)
        {
            double delta = end - start;
            return delta < 0.0 ? delta + Math.Tau : delta;
        }

        double clockwiseDelta = start - end;
        return clockwiseDelta < 0.0 ? clockwiseDelta + Math.Tau : clockwiseDelta;
    }

    private static double NormalizeRadians(double radians)
    {
        double value = radians % Math.Tau;
        return value < 0.0 ? value + Math.Tau : value;
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

            case EllipseEntity ellipse:
                IReadOnlyList<Point2D> points = ellipse.GetSamplePoints();
                for (int index = 0; index < points.Count; index++)
                {
                    yield return EntitySegment.FromLine(
                        new LineSegment2D(
                            points[index],
                            points[(index + 1) % points.Count]));
                }

                break;

            case EllipticalArcEntity ellipticalArc:
                IReadOnlyList<Point2D> ellipticalArcPoints = ellipticalArc.GetSamplePoints();
                for (int index = 0; index < ellipticalArcPoints.Count - 1; index++)
                {
                    yield return EntitySegment.FromLine(
                        new LineSegment2D(
                            ellipticalArcPoints[index],
                            ellipticalArcPoints[index + 1]));
                }

                break;

            case BezierSplineEntity spline:
                IReadOnlyList<Point2D> splinePoints = spline.GetSamplePoints();
                for (int index = 0; index < splinePoints.Count - 1; index++)
                {
                    yield return EntitySegment.FromLine(
                        new LineSegment2D(
                            splinePoints[index],
                            splinePoints[index + 1]));
                }

                if (spline.IsClosed && splinePoints.Count > 1)
                {
                    yield return EntitySegment.FromLine(
                        new LineSegment2D(
                            splinePoints[^1],
                            splinePoints[0]));
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
    private static CadIntersectionKind ClassifyIntersection(
        ICurveAdapter firstAdapter,
        ICurveAdapter secondAdapter,
        CurveCut firstCut,
        CurveCut secondCut,
        GeometryTolerance tolerance)
    {
        if (IsEndpoint(firstAdapter, firstCut.Parameter, tolerance) ||
            IsEndpoint(secondAdapter, secondCut.Parameter, tolerance))
        {
            return CadIntersectionKind.Endpoint;
        }

        return CadIntersectionKind.Crossing;
    }

    private static bool IsEndpoint(
        ICurveAdapter adapter,
        double parameter,
        GeometryTolerance tolerance)
    {
        if (adapter.IsClosed)
        {
            return false;
        }

        return Math.Abs(parameter - adapter.StartParameter) <= tolerance.Parameter ||
               Math.Abs(parameter - adapter.EndParameter) <= tolerance.Parameter;
    }

    private static void AddDistinct(
        List<CadIntersectionPoint> result,
        CadIntersectionPoint candidate,
        double tolerance)
    {
        if (result.Any(existing => existing.Point.DistanceTo(candidate.Point) <= tolerance))
        {
            return;
        }

        result.Add(candidate);
    }


}
