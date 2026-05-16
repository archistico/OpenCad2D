using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Provides break operations for supported CAD entities.
/// </summary>
public static class CadBreakService
{
    private const double FullCircleRadians = Math.PI * 2.0;

    public static IReadOnlyList<CadEntity> BreakAtPoint(
        CadEntity entity,
        Point2D breakPoint,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        return entity switch
        {
            LineEntity line => LineBreakService
                .BreakAtPoint(line, breakPoint, effectiveTolerance)
                .Cast<CadEntity>()
                .ToList(),

            ArcEntity arc => BreakArcAtPoint(
                arc,
                breakPoint,
                effectiveTolerance),

            EllipseEntity ellipse => BreakEllipseAtPoint(
                ellipse,
                breakPoint,
                effectiveTolerance),

            PolylineEntity polyline => BreakPolylineAtPoint(
                polyline,
                breakPoint,
                effectiveTolerance),

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
            LineEntity line => LineBreakService
                .BreakBetweenPoints(
                    line,
                    firstBreakPoint,
                    secondBreakPoint,
                    effectiveTolerance)
                .Cast<CadEntity>()
                .ToList(),

            ArcEntity arc => BreakArcBetweenPoints(
                arc,
                firstBreakPoint,
                secondBreakPoint,
                effectiveTolerance),

            CircleEntity circle => BreakCircleBetweenPoints(
                circle,
                firstBreakPoint,
                secondBreakPoint,
                effectiveTolerance),

            EllipseEntity ellipse => BreakEllipseBetweenPoints(
                ellipse,
                firstBreakPoint,
                secondBreakPoint,
                effectiveTolerance),

            PolylineEntity polyline => BreakPolylineBetweenPoints(
                polyline,
                firstBreakPoint,
                secondBreakPoint,
                effectiveTolerance),

            _ => Array.Empty<CadEntity>()
        };
    }

    private static IReadOnlyList<CadEntity> BreakArcAtPoint(
        ArcEntity arc,
        Point2D breakPoint,
        GeometryTolerance tolerance)
    {
        Point2D projectedPoint = arc.GetClosestPoint(breakPoint);

        if (!arc.Geometry.ContainsPoint(projectedPoint, tolerance.Distance))
        {
            return Array.Empty<CadEntity>();
        }

        Angle breakAngle = Angle.FromRadians(
            Math.Atan2(
                projectedPoint.Y - arc.Center.Y,
                projectedPoint.X - arc.Center.X));

        double parameter = GetArcParameter(
            arc.StartAngle,
            arc.EndAngle,
            breakAngle,
            arc.IsCounterClockwise);

        if (parameter <= tolerance.Parameter ||
            parameter >= 1.0 - tolerance.Parameter)
        {
            return Array.Empty<CadEntity>();
        }

        return new CadEntity[]
        {
            CreateArcLike(
                arc,
                arc.StartAngle,
                breakAngle),
            CreateArcLike(
                arc,
                breakAngle,
                arc.EndAngle)
        };
    }

    private static IReadOnlyList<CadEntity> BreakEllipseAtPoint(
        EllipseEntity ellipse,
        Point2D breakPoint,
        GeometryTolerance tolerance)
    {
        Point2D projectedPoint = ellipse.GetClosestPoint(breakPoint);

        double parameter = GetEllipseParameter(ellipse, projectedPoint);

        return new CadEntity[]
        {
            new PolylineEntity(
                CreateEllipsePolylineVertices(ellipse, parameter, parameter + FullCircleRadians),
                isClosed: false,
                layerId: ellipse.LayerId,
                style: ellipse.Style,
                isVisible: ellipse.IsVisible,
                isLocked: ellipse.IsLocked,
                drawOrder: ellipse.DrawOrder)
        };
    }

    private static IReadOnlyList<CadEntity> BreakPolylineAtPoint(
        PolylineEntity polyline,
        Point2D breakPoint,
        GeometryTolerance tolerance)
    {
        if (!TryProjectPointOnPolylineSegment(
                polyline,
                breakPoint,
                tolerance,
                out int segmentIndex,
                out double parameter,
                out Point2D projectedPoint))
        {
            return Array.Empty<CadEntity>();
        }

        if (polyline.IsClosed)
        {
            return BreakClosedPolylineAtPoint(
                polyline,
                segmentIndex,
                parameter,
                projectedPoint,
                tolerance);
        }

        return BreakOpenPolylineAtPoint(
            polyline,
            segmentIndex,
            parameter,
            projectedPoint,
            tolerance);
    }

    private static IReadOnlyList<CadEntity> BreakOpenPolylineAtPoint(
        PolylineEntity polyline,
        int segmentIndex,
        double parameter,
        Point2D projectedPoint,
        GeometryTolerance tolerance)
    {
        bool isAtStartEndpoint = segmentIndex == 0 &&
                                 parameter <= tolerance.Parameter;
        bool isAtEndEndpoint = segmentIndex == polyline.Vertices.Count - 2 &&
                               parameter >= 1.0 - tolerance.Parameter;

        if (isAtStartEndpoint || isAtEndEndpoint)
        {
            return Array.Empty<CadEntity>();
        }

        List<Point2D> firstVertices = polyline.Vertices
            .Take(segmentIndex + 1)
            .ToList();

        AddIfDifferent(
            firstVertices,
            projectedPoint,
            tolerance);

        List<Point2D> secondVertices = new();
        AddIfDifferent(
            secondVertices,
            projectedPoint,
            tolerance);

        secondVertices.AddRange(polyline.Vertices.Skip(segmentIndex + 1));

        var result = new List<CadEntity>();

        AddPolylineIfValid(
            result,
            polyline,
            firstVertices,
            isClosed: false,
            tolerance);

        AddPolylineIfValid(
            result,
            polyline,
            secondVertices,
            isClosed: false,
            tolerance);

        return result;
    }

    private static IReadOnlyList<CadEntity> BreakClosedPolylineAtPoint(
        PolylineEntity polyline,
        int segmentIndex,
        double parameter,
        Point2D projectedPoint,
        GeometryTolerance tolerance)
    {
        if (parameter <= tolerance.Parameter)
        {
            projectedPoint = polyline.Vertices[segmentIndex];
        }
        else if (parameter >= 1.0 - tolerance.Parameter)
        {
            projectedPoint = polyline.Vertices[(segmentIndex + 1) % polyline.Vertices.Count];
            segmentIndex = (segmentIndex + 1) % polyline.Vertices.Count;
        }

        var openedVertices = new List<Point2D>
        {
            projectedPoint
        };

        int vertexIndex = (segmentIndex + 1) % polyline.Vertices.Count;

        while (vertexIndex != segmentIndex)
        {
            AddIfDifferent(
                openedVertices,
                polyline.Vertices[vertexIndex],
                tolerance);

            vertexIndex = (vertexIndex + 1) % polyline.Vertices.Count;
        }

        AddIfDifferent(
            openedVertices,
            polyline.Vertices[segmentIndex],
            tolerance);

        AddIfDifferent(
            openedVertices,
            projectedPoint,
            tolerance);

        var result = new List<CadEntity>();

        AddPolylineIfValid(
            result,
            polyline,
            openedVertices,
            isClosed: false,
            tolerance);

        return result;
    }

    private static IReadOnlyList<CadEntity> BreakArcBetweenPoints(
        ArcEntity arc,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance tolerance)
    {
        Point2D firstProjectedPoint = arc.GetClosestPoint(firstBreakPoint);
        Point2D secondProjectedPoint = arc.GetClosestPoint(secondBreakPoint);

        if (!arc.Geometry.ContainsPoint(firstProjectedPoint, tolerance.Distance) ||
            !arc.Geometry.ContainsPoint(secondProjectedPoint, tolerance.Distance))
        {
            return Array.Empty<CadEntity>();
        }

        Angle firstAngle = Angle.FromRadians(
            Math.Atan2(
                firstProjectedPoint.Y - arc.Center.Y,
                firstProjectedPoint.X - arc.Center.X));

        Angle secondAngle = Angle.FromRadians(
            Math.Atan2(
                secondProjectedPoint.Y - arc.Center.Y,
                secondProjectedPoint.X - arc.Center.X));

        double firstParameter = GetArcParameter(
            arc.StartAngle,
            arc.EndAngle,
            firstAngle,
            arc.IsCounterClockwise);

        double secondParameter = GetArcParameter(
            arc.StartAngle,
            arc.EndAngle,
            secondAngle,
            arc.IsCounterClockwise);

        if (!tolerance.IsParameterWithinUnitInterval(firstParameter) ||
            !tolerance.IsParameterWithinUnitInterval(secondParameter))
        {
            return Array.Empty<CadEntity>();
        }

        double startBreak = Math.Clamp(Math.Min(firstParameter, secondParameter), 0.0, 1.0);
        double endBreak = Math.Clamp(Math.Max(firstParameter, secondParameter), 0.0, 1.0);

        if (tolerance.AreParametersEqual(startBreak, endBreak))
        {
            return Array.Empty<CadEntity>();
        }

        Angle startBreakAngle = AngleAtArcParameter(
            arc.StartAngle,
            arc.EndAngle,
            arc.IsCounterClockwise,
            startBreak);

        Angle endBreakAngle = AngleAtArcParameter(
            arc.StartAngle,
            arc.EndAngle,
            arc.IsCounterClockwise,
            endBreak);

        var result = new List<CadEntity>();

        AddArcIfValid(
            result,
            arc,
            arc.StartAngle,
            startBreakAngle,
            tolerance);

        AddArcIfValid(
            result,
            arc,
            endBreakAngle,
            arc.EndAngle,
            tolerance);

        return result;
    }

    private static IReadOnlyList<CadEntity> BreakCircleBetweenPoints(
        CircleEntity circle,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance tolerance)
    {
        Point2D firstProjectedPoint = circle.GetClosestPoint(firstBreakPoint);
        Point2D secondProjectedPoint = circle.GetClosestPoint(secondBreakPoint);

        if (firstProjectedPoint.DistanceTo(secondProjectedPoint) <= tolerance.Distance)
        {
            return Array.Empty<CadEntity>();
        }

        Angle firstAngle = Angle.FromRadians(
            Math.Atan2(
                firstProjectedPoint.Y - circle.Center.Y,
                firstProjectedPoint.X - circle.Center.X));

        Angle secondAngle = Angle.FromRadians(
            Math.Atan2(
                secondProjectedPoint.Y - circle.Center.Y,
                secondProjectedPoint.X - circle.Center.X));

        double counterClockwiseSweep = GetDirectedAngleDistance(
            firstAngle,
            secondAngle,
            isCounterClockwise: true);

        if (counterClockwiseSweep <= Math.PI)
        {
            return new CadEntity[]
            {
                new ArcEntity(
                    circle.Center,
                    circle.Radius,
                    secondAngle,
                    firstAngle,
                    isCounterClockwise: true,
                    layerId: circle.LayerId,
                    style: circle.Style,
                    isVisible: circle.IsVisible,
                    isLocked: circle.IsLocked,
                    drawOrder: circle.DrawOrder)
            };
        }

        return new CadEntity[]
        {
            new ArcEntity(
                circle.Center,
                circle.Radius,
                firstAngle,
                secondAngle,
                isCounterClockwise: true,
                layerId: circle.LayerId,
                style: circle.Style,
                isVisible: circle.IsVisible,
                isLocked: circle.IsLocked,
                drawOrder: circle.DrawOrder)
        };
    }

    private static IReadOnlyList<CadEntity> BreakEllipseBetweenPoints(
        EllipseEntity ellipse,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance tolerance)
    {
        Point2D firstProjectedPoint = ellipse.GetClosestPoint(firstBreakPoint);
        Point2D secondProjectedPoint = ellipse.GetClosestPoint(secondBreakPoint);

        if (firstProjectedPoint.DistanceTo(secondProjectedPoint) <= tolerance.Distance)
        {
            return Array.Empty<CadEntity>();
        }

        double firstParameter = GetEllipseParameter(ellipse, firstProjectedPoint);
        double secondParameter = GetEllipseParameter(ellipse, secondProjectedPoint);
        double counterClockwiseSweep = secondParameter >= firstParameter
            ? secondParameter - firstParameter
            : secondParameter + FullCircleRadians - firstParameter;

        double startParameter = firstParameter;
        double endParameter = secondParameter;

        if (counterClockwiseSweep <= Math.PI)
        {
            startParameter = secondParameter;
            endParameter = firstParameter + FullCircleRadians;
        }
        else
        {
            endParameter = secondParameter;
        }

        if (endParameter <= startParameter)
        {
            endParameter += FullCircleRadians;
        }

        return new CadEntity[]
        {
            new PolylineEntity(
                CreateEllipsePolylineVertices(ellipse, startParameter, endParameter),
                isClosed: false,
                layerId: ellipse.LayerId,
                style: ellipse.Style,
                isVisible: ellipse.IsVisible,
                isLocked: ellipse.IsLocked,
                drawOrder: ellipse.DrawOrder)
        };
    }

    private static IReadOnlyList<CadEntity> BreakPolylineBetweenPoints(
        PolylineEntity polyline,
        Point2D firstBreakPoint,
        Point2D secondBreakPoint,
        GeometryTolerance tolerance)
    {
        if (!TryProjectPointOnPolylineSegment(
                polyline,
                firstBreakPoint,
                tolerance,
                out int firstSegmentIndex,
                out double firstParameter,
                out Point2D firstProjectedPoint) ||
            !TryProjectPointOnPolylineSegment(
                polyline,
                secondBreakPoint,
                tolerance,
                out int secondSegmentIndex,
                out double secondParameter,
                out Point2D secondProjectedPoint))
        {
            return Array.Empty<CadEntity>();
        }

        if (firstProjectedPoint.DistanceTo(secondProjectedPoint) <= tolerance.Distance)
        {
            return Array.Empty<CadEntity>();
        }

        return polyline.IsClosed
            ? BreakClosedPolylineBetweenPoints(
                polyline,
                firstSegmentIndex,
                firstParameter,
                firstProjectedPoint,
                secondSegmentIndex,
                secondParameter,
                secondProjectedPoint,
                tolerance)
            : BreakOpenPolylineBetweenPoints(
                polyline,
                firstSegmentIndex,
                firstParameter,
                firstProjectedPoint,
                secondSegmentIndex,
                secondParameter,
                secondProjectedPoint,
                tolerance);
    }

    private static IReadOnlyList<CadEntity> BreakOpenPolylineBetweenPoints(
        PolylineEntity polyline,
        int firstSegmentIndex,
        double firstParameter,
        Point2D firstProjectedPoint,
        int secondSegmentIndex,
        double secondParameter,
        Point2D secondProjectedPoint,
        GeometryTolerance tolerance)
    {
        double firstPathParameter = firstSegmentIndex + firstParameter;
        double secondPathParameter = secondSegmentIndex + secondParameter;

        if (firstPathParameter > secondPathParameter)
        {
            (firstSegmentIndex, secondSegmentIndex) = (secondSegmentIndex, firstSegmentIndex);
            (firstParameter, secondParameter) = (secondParameter, firstParameter);
            (firstProjectedPoint, secondProjectedPoint) = (secondProjectedPoint, firstProjectedPoint);
        }

        var result = new List<CadEntity>();

        List<Point2D> firstPart = polyline.Vertices
            .Take(firstSegmentIndex + 1)
            .ToList();
        AddIfDifferent(firstPart, firstProjectedPoint, tolerance);

        List<Point2D> secondPart = new();
        AddIfDifferent(secondPart, secondProjectedPoint, tolerance);
        secondPart.AddRange(polyline.Vertices.Skip(secondSegmentIndex + 1));

        AddPolylineIfValid(
            result,
            polyline,
            firstPart,
            isClosed: false,
            tolerance);

        AddPolylineIfValid(
            result,
            polyline,
            secondPart,
            isClosed: false,
            tolerance);

        return result;
    }

    private static IReadOnlyList<CadEntity> BreakClosedPolylineBetweenPoints(
        PolylineEntity polyline,
        int firstSegmentIndex,
        double firstParameter,
        Point2D firstProjectedPoint,
        int secondSegmentIndex,
        double secondParameter,
        Point2D secondProjectedPoint,
        GeometryTolerance tolerance)
    {
        double firstPathParameter = firstSegmentIndex + firstParameter;
        double secondPathParameter = secondSegmentIndex + secondParameter;
        double segmentCount = polyline.Vertices.Count;

        double forwardLength = GetClosedPathDistance(
            polyline,
            firstSegmentIndex,
            firstParameter,
            firstProjectedPoint,
            secondSegmentIndex,
            secondParameter,
            secondProjectedPoint,
            tolerance);
        double totalLength = GetClosedPolylineLength(polyline);
        double backwardLength = totalLength - forwardLength;

        bool removeForward = forwardLength <= backwardLength;

        Point2D remainingStart = removeForward
            ? secondProjectedPoint
            : firstProjectedPoint;
        Point2D remainingEnd = removeForward
            ? firstProjectedPoint
            : secondProjectedPoint;
        double startParameter = removeForward
            ? secondPathParameter
            : firstPathParameter;
        double endParameter = removeForward
            ? firstPathParameter
            : secondPathParameter;

        if (startParameter >= segmentCount)
        {
            startParameter -= segmentCount;
        }

        if (endParameter >= segmentCount)
        {
            endParameter -= segmentCount;
        }

        List<Point2D> remainingVertices = BuildClosedPolylinePath(
            polyline,
            remainingStart,
            remainingEnd,
            startParameter,
            endParameter,
            tolerance);

        var result = new List<CadEntity>();

        AddPolylineIfValid(
            result,
            polyline,
            remainingVertices,
            isClosed: false,
            tolerance);

        return result;
    }

    private static List<Point2D> BuildClosedPolylinePath(
        PolylineEntity polyline,
        Point2D startPoint,
        Point2D endPoint,
        double startPathParameter,
        double endPathParameter,
        GeometryTolerance tolerance)
    {
        int vertexCount = polyline.Vertices.Count;
        int startSegmentIndex = (int)Math.Floor(startPathParameter) % vertexCount;
        int endSegmentIndex = (int)Math.Floor(endPathParameter) % vertexCount;

        var vertices = new List<Point2D>();
        AddIfDifferent(vertices, startPoint, tolerance);

        int intermediateVertexCount = endPathParameter >= startPathParameter
            ? endSegmentIndex - startSegmentIndex
            : vertexCount - startSegmentIndex + endSegmentIndex;

        int vertexIndex = (startSegmentIndex + 1) % vertexCount;

        for (int index = 0; index < intermediateVertexCount; index++)
        {
            AddIfDifferent(vertices, polyline.Vertices[vertexIndex], tolerance);
            vertexIndex = (vertexIndex + 1) % vertexCount;
        }

        AddIfDifferent(vertices, endPoint, tolerance);

        return vertices;
    }

    private static double GetClosedPathDistance(
        PolylineEntity polyline,
        int firstSegmentIndex,
        double firstParameter,
        Point2D firstProjectedPoint,
        int secondSegmentIndex,
        double secondParameter,
        Point2D secondProjectedPoint,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<LineSegment2D> segments = polyline.Geometry.GetSegments();

        if (firstSegmentIndex == secondSegmentIndex)
        {
            double segmentLength = segments[firstSegmentIndex].Start.DistanceTo(
                segments[firstSegmentIndex].End);
            double directDistance = Math.Abs(secondParameter - firstParameter) * segmentLength;

            return secondParameter >= firstParameter
                ? directDistance
                : GetClosedPolylineLength(polyline) - directDistance;
        }

        double distance = firstProjectedPoint.DistanceTo(segments[firstSegmentIndex].End);
        int segmentIndex = (firstSegmentIndex + 1) % segments.Count;

        while (segmentIndex != secondSegmentIndex)
        {
            distance += segments[segmentIndex].Start.DistanceTo(segments[segmentIndex].End);
            segmentIndex = (segmentIndex + 1) % segments.Count;
        }

        distance += segments[secondSegmentIndex].Start.DistanceTo(secondProjectedPoint);

        return distance;
    }

    private static bool TryProjectPointOnPolylineSegment(
        PolylineEntity polyline,
        Point2D point,
        GeometryTolerance tolerance,
        out int segmentIndex,
        out double parameter,
        out Point2D projectedPoint)
    {
        IReadOnlyList<LineSegment2D> segments = polyline.Geometry.GetSegments();
        double bestDistance = double.PositiveInfinity;
        int bestIndex = -1;
        double bestParameter = 0.0;
        Point2D bestPoint = default;

        for (int index = 0; index < segments.Count; index++)
        {
            LineSegment2D segment = segments[index];
            double currentParameter = LineParameterService.GetParameter(
                segment,
                point,
                tolerance);

            if (!LineIntersectionService.IsParameterOnSegment(
                    currentParameter,
                    tolerance))
            {
                continue;
            }

            double clampedParameter = Math.Clamp(currentParameter, 0.0, 1.0);
            Point2D currentPoint = LineParameterService.PointAt(
                segment,
                clampedParameter);
            double currentDistance = currentPoint.DistanceTo(point);

            if (currentDistance >= bestDistance)
            {
                continue;
            }

            bestDistance = currentDistance;
            bestIndex = index;
            bestParameter = clampedParameter;
            bestPoint = currentPoint;
        }

        if (bestIndex < 0)
        {
            segmentIndex = -1;
            parameter = 0.0;
            projectedPoint = default;
            return false;
        }

        segmentIndex = bestIndex;
        parameter = bestParameter;
        projectedPoint = bestPoint;
        return true;
    }

    private static void AddArcIfValid(
        ICollection<CadEntity> result,
        ArcEntity source,
        Angle startAngle,
        Angle endAngle,
        GeometryTolerance tolerance)
    {
        double sweep = GetDirectedAngleDistance(
            startAngle,
            endAngle,
            source.IsCounterClockwise);

        if (sweep <= tolerance.Angle)
        {
            return;
        }

        result.Add(
            CreateArcLike(
                source,
                startAngle,
                endAngle));
    }

    private static Angle AngleAtArcParameter(
        Angle startAngle,
        Angle endAngle,
        bool isCounterClockwise,
        double parameter)
    {
        double sweep = GetDirectedAngleDistance(
            startAngle,
            endAngle,
            isCounterClockwise);
        double signedSweep = isCounterClockwise
            ? sweep
            : -sweep;

        return Angle.FromRadians(startAngle.Radians + signedSweep * parameter);
    }

    private static double GetClosedPolylineLength(PolylineEntity polyline)
    {
        return polyline.Geometry.GetSegments()
            .Sum(segment => segment.Start.DistanceTo(segment.End));
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

    private static IReadOnlyList<Point2D> CreateEllipsePolylineVertices(
        EllipseEntity ellipse,
        double startParameter,
        double endParameter)
    {
        double sweep = endParameter - startParameter;
        int segmentCount = Math.Max(
            2,
            (int)Math.Ceiling(EllipseEntity.DefaultSampleCount * sweep / FullCircleRadians));

        var vertices = new List<Point2D>(segmentCount + 1);
        for (int index = 0; index <= segmentCount; index++)
        {
            double parameter = startParameter + sweep * index / segmentCount;
            vertices.Add(ellipse.GetPointAt(parameter));
        }

        return vertices;
    }

    private static ArcEntity CreateArcLike(
        ArcEntity source,
        Angle startAngle,
        Angle endAngle)
    {
        return new ArcEntity(
            source.Center,
            source.Radius,
            startAngle,
            endAngle,
            source.IsCounterClockwise,
            layerId: source.LayerId,
            style: source.Style,
            isVisible: source.IsVisible,
            isLocked: source.IsLocked,
            drawOrder: source.DrawOrder);
    }

    private static void AddPolylineIfValid(
        ICollection<CadEntity> result,
        PolylineEntity source,
        IReadOnlyList<Point2D> vertices,
        bool isClosed,
        GeometryTolerance tolerance)
    {
        var cleanedVertices = RemoveConsecutiveDuplicates(
            vertices,
            tolerance);

        if (cleanedVertices.Count < 2)
        {
            return;
        }

        double length = 0.0;

        for (int index = 0; index < cleanedVertices.Count - 1; index++)
        {
            length += cleanedVertices[index].DistanceTo(cleanedVertices[index + 1]);
        }

        if (length <= tolerance.Distance)
        {
            return;
        }

        result.Add(
            new PolylineEntity(
                cleanedVertices,
                isClosed,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder));
    }

    private static List<Point2D> RemoveConsecutiveDuplicates(
        IReadOnlyList<Point2D> vertices,
        GeometryTolerance tolerance)
    {
        var result = new List<Point2D>();

        foreach (Point2D vertex in vertices)
        {
            AddIfDifferent(
                result,
                vertex,
                tolerance);
        }

        return result;
    }

    private static void AddIfDifferent(
        ICollection<Point2D> vertices,
        Point2D point,
        GeometryTolerance tolerance)
    {
        if (vertices.LastOrDefault().DistanceTo(point) <= tolerance.Distance &&
            vertices.Count > 0)
        {
            return;
        }

        vertices.Add(point);
    }

    private static double GetArcParameter(
        Angle startAngle,
        Angle endAngle,
        Angle valueAngle,
        bool isCounterClockwise)
    {
        double sweep = GetDirectedAngleDistance(
            startAngle,
            endAngle,
            isCounterClockwise);
        double value = GetDirectedAngleDistance(
            startAngle,
            valueAngle,
            isCounterClockwise);

        if (sweep <= 0.0)
        {
            return 0.0;
        }

        return value / sweep;
    }

    private static double NormalizeRadians(double radians)
    {
        double value = radians % FullCircleRadians;
        return value < 0 ? value + FullCircleRadians : value;
    }

    private static double GetDirectedAngleDistance(
        Angle startAngle,
        Angle endAngle,
        bool isCounterClockwise)
    {
        double start = startAngle.NormalizePositive().Radians;
        double end = endAngle.NormalizePositive().Radians;

        if (isCounterClockwise)
        {
            double delta = end - start;
            return delta < 0.0
                ? delta + FullCircleRadians
                : delta;
        }

        double clockwiseDelta = start - end;
        return clockwiseDelta < 0.0
            ? clockwiseDelta + FullCircleRadians
            : clockwiseDelta;
    }
}
