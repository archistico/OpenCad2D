using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides extend operations for editable CAD entities.
/// </summary>
public static class CadExtendService
{
    public static CadEntity? ExtendToBoundary(
        CadEntity target,
        CadEntity boundary,
        Point2D targetPickPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(boundary);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return target switch
        {
            LineEntity line => ExtendLine(line, boundary, targetPickPoint, effectiveTolerance),
            ArcEntity arc => ExtendArc(arc, boundary, targetPickPoint, effectiveTolerance),
            EllipticalArcEntity ellipticalArc => ExtendEllipticalArc(
                ellipticalArc,
                boundary,
                targetPickPoint,
                effectiveTolerance),
            PolylineEntity polyline when !polyline.IsClosed => ExtendPolyline(
                polyline,
                boundary,
                targetPickPoint,
                effectiveTolerance),
            _ => null
        };
    }

    private static CadEntity? ExtendLine(
        LineEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        Line2D infiniteLine = Line2D.FromPoints(target.Start, target.End);
        IReadOnlyList<Point2D> intersections = CadEntityIntersectionService
            .IntersectInfiniteLineWithEntity(infiniteLine, boundary, tolerance);

        if (intersections.Count == 0)
        {
            return null;
        }

        bool pickedStart = pickPoint.DistanceTo(target.Start) <=
                           pickPoint.DistanceTo(target.End);

        Point2D? candidate = pickedStart
            ? FindBestPointBeforeStart(target.Geometry, intersections, tolerance)
            : FindBestPointAfterEnd(target.Geometry, intersections, tolerance);

        if (candidate is null)
        {
            return null;
        }

        return new LineEntity(
            pickedStart ? candidate.Value : target.Start,
            pickedStart ? target.End : candidate.Value,
            target.Id,
            target.LayerId,
            target.Style,
            target.IsVisible,
            target.IsLocked,
            target.DrawOrder);
    }

    private static CadEntity? ExtendArc(
        ArcEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        Circle2D fullCircle = new(target.Center, target.Radius);
        IReadOnlyList<Point2D> intersections = CadEntityIntersectionService
            .IntersectCircleWithEntity(fullCircle, boundary, tolerance);

        if (intersections.Count == 0)
        {
            return null;
        }

        bool pickedStart = pickPoint.DistanceTo(target.Geometry.StartPoint) <=
                           pickPoint.DistanceTo(target.Geometry.EndPoint);

        Point2D? candidate = FindBestArcExtensionPoint(
            target.Geometry,
            intersections,
            pickedStart,
            tolerance);

        if (candidate is null)
        {
            return null;
        }

        Angle candidateAngle = Angle.FromRadians(Math.Atan2(
            candidate.Value.Y - target.Center.Y,
            candidate.Value.X - target.Center.X));

        return new ArcEntity(
            target.Center,
            target.Radius,
            pickedStart ? candidateAngle : target.StartAngle,
            pickedStart ? target.EndAngle : candidateAngle,
            target.IsCounterClockwise,
            target.Id,
            target.LayerId,
            target.Style,
            target.IsVisible,
            target.IsLocked,
            target.DrawOrder);
    }

    private static CadEntity? ExtendEllipticalArc(
        EllipticalArcEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        var fullEllipse = new EllipseEntity(
            target.Center,
            target.MajorAxis,
            target.MinorRadius);

        IReadOnlyList<Point2D> intersections = CadEntityIntersectionService
            .Intersect(fullEllipse, boundary, tolerance);

        if (intersections.Count == 0)
        {
            return null;
        }

        bool pickedStart = pickPoint.DistanceTo(target.StartPoint) <=
                           pickPoint.DistanceTo(target.EndPoint);

        Point2D? candidate = FindBestEllipticalArcExtensionPoint(
            target,
            intersections,
            pickedStart,
            tolerance);

        if (candidate is null)
        {
            return null;
        }

        double candidateParameter = GetEllipseParameter(
            target.Center,
            target.MajorDirection,
            target.MajorRadius,
            target.MinorAxis.Normalize(),
            target.MinorRadius,
            candidate.Value);

        return new EllipticalArcEntity(
            target.Center,
            target.MajorAxis,
            target.MinorRadius,
            pickedStart ? candidateParameter : target.StartParameterRadians,
            pickedStart ? target.EndParameterRadians : candidateParameter,
            target.IsCounterClockwise,
            target.Id,
            target.LayerId,
            target.Style,
            target.IsVisible,
            target.IsLocked,
            target.DrawOrder);
    }

    private static CadEntity? ExtendPolyline(
        PolylineEntity target,
        CadEntity boundary,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> vertices = target.Vertices;

        bool pickedStart = pickPoint.DistanceTo(vertices[0]) <=
                           pickPoint.DistanceTo(vertices[^1]);

        Point2D fixedPoint = pickedStart ? vertices[1] : vertices[^2];
        Point2D endpoint = pickedStart ? vertices[0] : vertices[^1];

        Line2D infiniteLine = Line2D.FromPoints(fixedPoint, endpoint);
        LineSegment2D endpointSegment = pickedStart
            ? new LineSegment2D(vertices[1], vertices[0])
            : new LineSegment2D(vertices[^2], vertices[^1]);

        IReadOnlyList<Point2D> intersections = CadEntityIntersectionService
            .IntersectInfiniteLineWithEntity(infiniteLine, boundary, tolerance);

        if (intersections.Count == 0)
        {
            return null;
        }

        Point2D? candidate = FindBestPointAfterEnd(
            endpointSegment,
            intersections,
            tolerance);

        if (candidate is null)
        {
            return null;
        }

        var newVertices = vertices.ToList();

        if (pickedStart)
        {
            newVertices[0] = candidate.Value;
        }
        else
        {
            newVertices[^1] = candidate.Value;
        }

        return new PolylineEntity(
            newVertices,
            isClosed: false,
            target.Id,
            target.LayerId,
            target.Style,
            target.IsVisible,
            target.IsLocked,
            target.DrawOrder);
    }

    private static Point2D? FindBestPointBeforeStart(
        LineSegment2D target,
        IReadOnlyList<Point2D> intersections,
        GeometryTolerance tolerance)
    {
        return intersections
            .Select(point => new
            {
                Point = point,
                Parameter = LineParameterService.GetParameter(target, point, tolerance)
            })
            .Where(item => item.Parameter < -tolerance.Parameter)
            .OrderByDescending(item => item.Parameter)
            .Select(item => (Point2D?)item.Point)
            .FirstOrDefault();
    }

    private static Point2D? FindBestPointAfterEnd(
        LineSegment2D target,
        IReadOnlyList<Point2D> intersections,
        GeometryTolerance tolerance)
    {
        return intersections
            .Select(point => new
            {
                Point = point,
                Parameter = LineParameterService.GetParameter(target, point, tolerance)
            })
            .Where(item => item.Parameter > 1.0 + tolerance.Parameter)
            .OrderBy(item => item.Parameter)
            .Select(item => (Point2D?)item.Point)
            .FirstOrDefault();
    }

    private static Point2D? FindBestArcExtensionPoint(
        Arc2D arc,
        IReadOnlyList<Point2D> intersections,
        bool pickedStart,
        GeometryTolerance tolerance)
    {
        return intersections
            .Where(point => !arc.ContainsPoint(point, tolerance.Distance))
            .Select(point => new
            {
                Point = point,
                Distance = GetAngularExtensionDistance(arc, point, pickedStart)
            })
            .Where(item => item.Distance > tolerance.Angle)
            .OrderBy(item => item.Distance)
            .Select(item => (Point2D?)item.Point)
            .FirstOrDefault();
    }

    private static Point2D? FindBestEllipticalArcExtensionPoint(
        EllipticalArcEntity arc,
        IReadOnlyList<Point2D> intersections,
        bool pickedStart,
        GeometryTolerance tolerance)
    {
        return intersections
            .Select(point => new
            {
                Point = point,
                Parameter = GetEllipseParameter(
                    arc.Center,
                    arc.MajorDirection,
                    arc.MajorRadius,
                    arc.MinorAxis.Normalize(),
                    arc.MinorRadius,
                    point)
            })
            .Where(item => !IsParameterOnEllipticalArc(arc, item.Parameter, tolerance))
            .Select(item => new
            {
                item.Point,
                Distance = GetEllipticalArcExtensionDistance(arc, item.Parameter, pickedStart)
            })
            .Where(item => item.Distance > tolerance.Angle)
            .OrderBy(item => item.Distance)
            .Select(item => (Point2D?)item.Point)
            .FirstOrDefault();
    }

    private static bool IsParameterOnEllipticalArc(
        EllipticalArcEntity arc,
        double parameter,
        GeometryTolerance tolerance)
    {
        double distanceFromStart = GetDirectedParameterDistance(
            arc.StartParameterRadians,
            parameter,
            arc.IsCounterClockwise);

        return distanceFromStart >= -tolerance.Angle &&
               distanceFromStart <= arc.SweepRadians + tolerance.Angle;
    }

    private static double GetEllipticalArcExtensionDistance(
        EllipticalArcEntity arc,
        double parameter,
        bool pickedStart)
    {
        if (pickedStart)
        {
            return arc.IsCounterClockwise
                ? ClockwiseDistance(arc.StartParameterRadians, parameter)
                : CounterClockwiseDistance(arc.StartParameterRadians, parameter);
        }

        return arc.IsCounterClockwise
            ? CounterClockwiseDistance(arc.EndParameterRadians, parameter)
            : ClockwiseDistance(arc.EndParameterRadians, parameter);
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

        return isCounterClockwise
            ? CounterClockwiseDistance(start, end)
            : ClockwiseDistance(start, end);
    }

    private static double GetAngularExtensionDistance(
        Arc2D arc,
        Point2D point,
        bool pickedStart)
    {
        double value = NormalizeRadians(Math.Atan2(
            point.Y - arc.Center.Y,
            point.X - arc.Center.X));
        double start = NormalizeRadians(arc.StartAngle.Radians);
        double end = NormalizeRadians(arc.EndAngle.Radians);

        if (pickedStart)
        {
            return arc.IsCounterClockwise
                ? ClockwiseDistance(start, value)
                : CounterClockwiseDistance(start, value);
        }

        return arc.IsCounterClockwise
            ? CounterClockwiseDistance(end, value)
            : ClockwiseDistance(end, value);
    }

    private static double CounterClockwiseDistance(double from, double to)
    {
        return to >= from
            ? to - from
            : to + TwoPi - from;
    }

    private static double ClockwiseDistance(double from, double to)
    {
        return from >= to
            ? from - to
            : from + TwoPi - to;
    }

    private static double NormalizeRadians(double radians)
    {
        double value = radians % TwoPi;
        return value < 0 ? value + TwoPi : value;
    }

    private const double TwoPi = Math.PI * 2.0;
}
