using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Editing;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing.Curves;

/// <summary>
/// Creates native curve adapters for entities currently supported by the shared split pipeline.
/// </summary>
public sealed class DefaultCurveAdapterFactory : ICurveAdapterFactory
{
    public bool TryCreate(
        CadEntity entity,
        out ICurveAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(entity);

        adapter = entity switch
        {
            LineEntity line => new LineCurveAdapter(line),
            CircleEntity circle => new CircleCurveAdapter(circle),
            ArcEntity arc => new ArcCurveAdapter(arc),
            EllipseEntity ellipse => new EllipseCurveAdapter(ellipse),
            EllipticalArcEntity ellipticalArc => new EllipticalArcCurveAdapter(ellipticalArc),
            PolylineEntity polyline => new PolylineCurveAdapter(polyline),
            BezierSplineEntity spline => new BezierSplineCurveAdapter(spline),
            _ => null!
        };

        return adapter is not null;
    }

    private sealed class LineCurveAdapter : ICurveAdapter
    {
        private readonly LineEntity _line;

        public LineCurveAdapter(LineEntity line)
        {
            _line = line;
        }

        public CadEntity Source => _line;

        public bool IsClosed => false;

        public double StartParameter => 0.0;

        public double EndParameter => 1.0;

        public double Period => 0.0;

        public Point2D PointAt(double parameter)
        {
            double clamped = Math.Clamp(parameter, 0.0, 1.0);
            Vector2D direction = _line.Start.VectorTo(_line.End);

            return new Point2D(
                _line.Start.X + direction.X * clamped,
                _line.Start.Y + direction.Y * clamped);
        }

        public bool TryProjectPointToCut(
            Point2D point,
            GeometryTolerance tolerance,
            out CurveCut cut)
        {
            Point2D projectedPoint = _line.GetClosestPoint(point);

            double parameter = LineParameterService.GetParameter(
                _line.Geometry,
                projectedPoint,
                tolerance);

            if (!tolerance.IsParameterWithinUnitInterval(parameter))
            {
                cut = default;
                return false;
            }

            cut = new CurveCut(Math.Clamp(parameter, 0.0, 1.0), projectedPoint);
            return true;
        }

        public IReadOnlyList<CadEntity> BuildFragments(
            IReadOnlyList<CurveInterval> intervalsToKeep,
            GeometryTolerance tolerance)
        {
            var result = new List<CadEntity>();

            foreach (CurveInterval interval in intervalsToKeep)
            {
                Point2D start = interval.Start.Point;
                Point2D end = interval.End.Point;

                if (start.DistanceTo(end) <= tolerance.Distance)
                {
                    continue;
                }

                result.Add(new LineEntity(
                    start,
                    end,
                    layerId: _line.LayerId,
                    style: _line.Style,
                    isVisible: _line.IsVisible,
                    isLocked: _line.IsLocked,
                    drawOrder: _line.DrawOrder));
            }

            return result;
        }
    }

    private sealed class CircleCurveAdapter : ICurveAdapter
    {
        private const double TwoPi = Math.PI * 2.0;
        private readonly CircleEntity _circle;

        public CircleCurveAdapter(CircleEntity circle)
        {
            _circle = circle;
        }

        public CadEntity Source => _circle;

        public bool IsClosed => true;

        public double StartParameter => 0.0;

        public double EndParameter => TwoPi;

        public double Period => TwoPi;

        public Point2D PointAt(double parameter)
        {
            double angle = NormalizeRadians(parameter);

            return new Point2D(
                _circle.Center.X + Math.Cos(angle) * _circle.Radius,
                _circle.Center.Y + Math.Sin(angle) * _circle.Radius);
        }

        public bool TryProjectPointToCut(
            Point2D point,
            GeometryTolerance tolerance,
            out CurveCut cut)
        {
            Point2D projectedPoint = _circle.GetClosestPoint(point);

            cut = new CurveCut(
                NormalizeRadians(Math.Atan2(
                    projectedPoint.Y - _circle.Center.Y,
                    projectedPoint.X - _circle.Center.X)),
                projectedPoint);
            return true;
        }

        public IReadOnlyList<CadEntity> BuildFragments(
            IReadOnlyList<CurveInterval> intervalsToKeep,
            GeometryTolerance tolerance)
        {
            var result = new List<CadEntity>();

            foreach (CurveInterval interval in intervalsToKeep)
            {
                double start = NormalizeRadians(interval.Start.Parameter);
                double end = interval.End.Parameter;

                if (end <= interval.Start.Parameter)
                {
                    end += TwoPi;
                }

                if (end - interval.Start.Parameter <= tolerance.Angle)
                {
                    continue;
                }

                result.Add(new ArcEntity(
                    _circle.Center,
                    _circle.Radius,
                    Angle.FromRadians(start),
                    Angle.FromRadians(NormalizeRadians(end)),
                    isCounterClockwise: true,
                    layerId: _circle.LayerId,
                    style: _circle.Style,
                    isVisible: _circle.IsVisible,
                    isLocked: _circle.IsLocked,
                    drawOrder: _circle.DrawOrder));
            }

            return result;
        }
    }

    private sealed class ArcCurveAdapter : ICurveAdapter
    {
        private readonly ArcEntity _arc;

        public ArcCurveAdapter(ArcEntity arc)
        {
            _arc = arc;
        }

        public CadEntity Source => _arc;

        public bool IsClosed => false;

        public double StartParameter => 0.0;

        public double EndParameter => 1.0;

        public double Period => 0.0;

        public Point2D PointAt(double parameter)
        {
            Angle angle = AngleAtParameter(Math.Clamp(parameter, 0.0, 1.0));

            return new Point2D(
                _arc.Center.X + Math.Cos(angle.Radians) * _arc.Radius,
                _arc.Center.Y + Math.Sin(angle.Radians) * _arc.Radius);
        }

        public bool TryProjectPointToCut(
            Point2D point,
            GeometryTolerance tolerance,
            out CurveCut cut)
        {
            Point2D projectedPoint = _arc.GetClosestPoint(point);

            if (!_arc.Geometry.ContainsPoint(projectedPoint, tolerance.Distance))
            {
                cut = default;
                return false;
            }

            Angle angle = Angle.FromRadians(Math.Atan2(
                projectedPoint.Y - _arc.Center.Y,
                projectedPoint.X - _arc.Center.X));
            double parameter = GetArcParameter(
                _arc.StartAngle,
                _arc.EndAngle,
                angle,
                _arc.IsCounterClockwise);

            if (!tolerance.IsParameterWithinUnitInterval(parameter))
            {
                cut = default;
                return false;
            }

            cut = new CurveCut(Math.Clamp(parameter, 0.0, 1.0), projectedPoint);
            return true;
        }

        public IReadOnlyList<CadEntity> BuildFragments(
            IReadOnlyList<CurveInterval> intervalsToKeep,
            GeometryTolerance tolerance)
        {
            var result = new List<CadEntity>();

            foreach (CurveInterval interval in intervalsToKeep)
            {
                double start = Math.Clamp(interval.Start.Parameter, 0.0, 1.0);
                double end = Math.Clamp(interval.End.Parameter, 0.0, 1.0);

                if (end - start <= tolerance.Parameter)
                {
                    continue;
                }

                result.Add(new ArcEntity(
                    _arc.Center,
                    _arc.Radius,
                    AngleAtParameter(start),
                    AngleAtParameter(end),
                    _arc.IsCounterClockwise,
                    layerId: _arc.LayerId,
                    style: _arc.Style,
                    isVisible: _arc.IsVisible,
                    isLocked: _arc.IsLocked,
                    drawOrder: _arc.DrawOrder));
            }

            return result;
        }

        private Angle AngleAtParameter(double parameter)
        {
            double sweep = GetDirectedAngleDistance(
                _arc.StartAngle,
                _arc.EndAngle,
                _arc.IsCounterClockwise);
            double signedSweep = _arc.IsCounterClockwise
                ? sweep
                : -sweep;

            return Angle.FromRadians(_arc.StartAngle.Radians + signedSweep * parameter);
        }
    }



    private sealed class EllipseCurveAdapter : ICurveAdapter
    {
        private const double TwoPi = Math.PI * 2.0;
        private readonly EllipseEntity _ellipse;

        public EllipseCurveAdapter(EllipseEntity ellipse)
        {
            _ellipse = ellipse;
        }

        public CadEntity Source => _ellipse;

        public bool IsClosed => true;

        public double StartParameter => 0.0;

        public double EndParameter => TwoPi;

        public double Period => TwoPi;

        public Point2D PointAt(double parameter)
        {
            return _ellipse.GetPointAt(parameter);
        }

        public bool TryProjectPointToCut(
            Point2D point,
            GeometryTolerance tolerance,
            out CurveCut cut)
        {
            double parameter = GetEllipseParameter(
                _ellipse.Center,
                _ellipse.MajorDirection,
                _ellipse.MajorRadius,
                _ellipse.MinorAxis.Normalize(),
                _ellipse.MinorRadius,
                point);
            Point2D projectedPoint = _ellipse.GetPointAt(parameter);

            if (projectedPoint.DistanceTo(point) > tolerance.Distance)
            {
                projectedPoint = _ellipse.GetClosestPoint(point);
                parameter = GetEllipseParameter(
                    _ellipse.Center,
                    _ellipse.MajorDirection,
                    _ellipse.MajorRadius,
                    _ellipse.MinorAxis.Normalize(),
                    _ellipse.MinorRadius,
                    projectedPoint);
            }

            cut = new CurveCut(
                NormalizeRadians(parameter),
                projectedPoint);
            return true;
        }

        public IReadOnlyList<CadEntity> BuildFragments(
            IReadOnlyList<CurveInterval> intervalsToKeep,
            GeometryTolerance tolerance)
        {
            var result = new List<CadEntity>();

            foreach (CurveInterval interval in intervalsToKeep)
            {
                double start = NormalizeRadians(interval.Start.Parameter);
                double end = interval.End.Parameter;

                if (end <= interval.Start.Parameter)
                {
                    end += TwoPi;
                }

                if (end - interval.Start.Parameter <= tolerance.Angle)
                {
                    continue;
                }

                result.Add(new EllipticalArcEntity(
                    _ellipse.Center,
                    _ellipse.MajorAxis,
                    _ellipse.MinorRadius,
                    start,
                    NormalizeRadians(end),
                    isCounterClockwise: true,
                    layerId: _ellipse.LayerId,
                    style: _ellipse.Style,
                    isVisible: _ellipse.IsVisible,
                    isLocked: _ellipse.IsLocked,
                    drawOrder: _ellipse.DrawOrder));
            }

            return result;
        }
    }

    private sealed class EllipticalArcCurveAdapter : ICurveAdapter
    {
        private readonly EllipticalArcEntity _arc;

        public EllipticalArcCurveAdapter(EllipticalArcEntity arc)
        {
            _arc = arc;
        }

        public CadEntity Source => _arc;

        public bool IsClosed => false;

        public double StartParameter => 0.0;

        public double EndParameter => 1.0;

        public double Period => 0.0;

        public Point2D PointAt(double parameter)
        {
            double clamped = Math.Clamp(parameter, 0.0, 1.0);
            return _arc.GetPointAt(ParameterAt(clamped));
        }

        public bool TryProjectPointToCut(
            Point2D point,
            GeometryTolerance tolerance,
            out CurveCut cut)
        {
            double ellipseParameter = GetEllipseParameter(
                _arc.Center,
                _arc.MajorDirection,
                _arc.MajorRadius,
                _arc.MinorAxis.Normalize(),
                _arc.MinorRadius,
                point);
            Point2D projectedPoint = _arc.GetPointAt(ellipseParameter);

            if (projectedPoint.DistanceTo(point) > tolerance.Distance)
            {
                projectedPoint = _arc.GetClosestPoint(point);
                ellipseParameter = GetEllipseParameter(
                    _arc.Center,
                    _arc.MajorDirection,
                    _arc.MajorRadius,
                    _arc.MinorAxis.Normalize(),
                    _arc.MinorRadius,
                    projectedPoint);
            }

            double parameter = GetEllipticalArcParameter(
                _arc.StartParameterRadians,
                _arc.EndParameterRadians,
                NormalizeRadians(ellipseParameter),
                _arc.IsCounterClockwise);

            if (!tolerance.IsParameterWithinUnitInterval(parameter))
            {
                cut = default;
                return false;
            }

            cut = new CurveCut(Math.Clamp(parameter, 0.0, 1.0), projectedPoint);
            return true;
        }

        public IReadOnlyList<CadEntity> BuildFragments(
            IReadOnlyList<CurveInterval> intervalsToKeep,
            GeometryTolerance tolerance)
        {
            var result = new List<CadEntity>();

            foreach (CurveInterval interval in intervalsToKeep)
            {
                double start = Math.Clamp(interval.Start.Parameter, 0.0, 1.0);
                double end = Math.Clamp(interval.End.Parameter, 0.0, 1.0);

                if (end - start <= tolerance.Parameter)
                {
                    continue;
                }

                result.Add(new EllipticalArcEntity(
                    _arc.Center,
                    _arc.MajorAxis,
                    _arc.MinorRadius,
                    ParameterAt(start),
                    ParameterAt(end),
                    _arc.IsCounterClockwise,
                    layerId: _arc.LayerId,
                    style: _arc.Style,
                    isVisible: _arc.IsVisible,
                    isLocked: _arc.IsLocked,
                    drawOrder: _arc.DrawOrder));
            }

            return result;
        }

        private double ParameterAt(double parameter)
        {
            double signedSweep = _arc.IsCounterClockwise
                ? _arc.SweepRadians
                : -_arc.SweepRadians;

            return NormalizeRadians(_arc.StartParameterRadians + signedSweep * parameter);
        }
    }



    private sealed class BezierSplineCurveAdapter : ICurveAdapter
    {
        private const int ProjectionSampleCount = 128;
        private readonly BezierSplineEntity _spline;
        private readonly BezierSplineSplitService _splitService = new();

        public BezierSplineCurveAdapter(BezierSplineEntity spline)
        {
            _spline = spline;
        }

        public CadEntity Source => _spline;

        public bool IsClosed => _spline.IsClosed;

        public double StartParameter => 0.0;

        public double EndParameter => 1.0;

        public double Period => _spline.IsClosed ? 1.0 : 0.0;

        public Point2D PointAt(double parameter)
        {
            return BezierSplineSplitService.Evaluate(
                _spline,
                Math.Clamp(parameter, 0.0, 1.0));
        }

        public bool TryProjectPointToCut(
            Point2D point,
            GeometryTolerance tolerance,
            out CurveCut cut)
        {
            if (_spline.IsClosed)
            {
                cut = default;
                return false;
            }

            double parameter = FindClosestParameter(point);
            Point2D projectedPoint = PointAt(parameter);

            cut = new CurveCut(parameter, projectedPoint);
            return true;
        }

        public IReadOnlyList<CadEntity> BuildFragments(
            IReadOnlyList<CurveInterval> intervalsToKeep,
            GeometryTolerance tolerance)
        {
            if (_spline.IsClosed)
            {
                return Array.Empty<CadEntity>();
            }

            var result = new List<CadEntity>();

            foreach (CurveInterval interval in intervalsToKeep)
            {
                double start = Math.Clamp(interval.Start.Parameter, 0.0, 1.0);
                double end = Math.Clamp(interval.End.Parameter, 0.0, 1.0);

                if (end - start <= tolerance.Parameter)
                {
                    continue;
                }

                BezierSplineEntity? fragment = _splitService.ExtractInterval(
                    _spline,
                    start,
                    end,
                    tolerance);

                if (fragment is not null)
                {
                    result.Add(fragment);
                }
            }

            return result;
        }

        private double FindClosestParameter(Point2D point)
        {
            double bestParameter = 0.0;
            double bestDistanceSquared = double.PositiveInfinity;

            for (int index = 0; index <= ProjectionSampleCount; index++)
            {
                double parameter = (double)index / ProjectionSampleCount;
                double distanceSquared = DistanceSquared(PointAt(parameter), point);

                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestParameter = parameter;
                }
            }

            double window = 1.0 / ProjectionSampleCount;
            double left = Math.Max(0.0, bestParameter - window);
            double right = Math.Min(1.0, bestParameter + window);

            for (int iteration = 0; iteration < 48; iteration++)
            {
                double first = left + ((right - left) / 3.0);
                double second = right - ((right - left) / 3.0);

                if (DistanceSquared(PointAt(first), point) <= DistanceSquared(PointAt(second), point))
                {
                    right = second;
                }
                else
                {
                    left = first;
                }
            }

            return Math.Clamp((left + right) / 2.0, 0.0, 1.0);
        }
    }


    private sealed class PolylineCurveAdapter : ICurveAdapter
    {
        private readonly PolylineEntity _polyline;
        private readonly IReadOnlyList<LineSegment2D> _segments;
        private readonly double[] _segmentStarts;
        private readonly double _totalLength;

        public PolylineCurveAdapter(PolylineEntity polyline)
        {
            _polyline = polyline;
            _segments = polyline.Geometry.GetSegments();
            _segmentStarts = new double[_segments.Count];

            double accumulated = 0.0;
            for (int index = 0; index < _segments.Count; index++)
            {
                _segmentStarts[index] = accumulated;
                accumulated += _segments[index].Length;
            }

            _totalLength = accumulated;
        }

        public CadEntity Source => _polyline;

        public bool IsClosed => _polyline.IsClosed;

        public double StartParameter => 0.0;

        public double EndParameter => _totalLength;

        public double Period => _polyline.IsClosed ? _totalLength : 0.0;

        public Point2D PointAt(double parameter)
        {
            if (_segments.Count == 0)
            {
                return _polyline.Vertices[0];
            }

            double effectiveParameter = _polyline.IsClosed
                ? NormalizePeriodic(parameter, _totalLength)
                : Math.Clamp(parameter, 0.0, _totalLength);

            if (!_polyline.IsClosed && effectiveParameter >= _totalLength)
            {
                return _polyline.Vertices[^1];
            }

            int segmentIndex = FindSegmentIndex(effectiveParameter);
            LineSegment2D segment = _segments[segmentIndex];
            double segmentLength = segment.Length;

            if (segmentLength <= 0.0)
            {
                return segment.Start;
            }

            double localParameter = (effectiveParameter - _segmentStarts[segmentIndex]) / segmentLength;

            return LineParameterService.PointAt(
                segment,
                Math.Clamp(localParameter, 0.0, 1.0));
        }

        public bool TryProjectPointToCut(
            Point2D point,
            GeometryTolerance tolerance,
            out CurveCut cut)
        {
            double bestDistance = double.PositiveInfinity;
            int bestIndex = -1;
            double bestLocalParameter = 0.0;
            Point2D bestPoint = default;

            for (int index = 0; index < _segments.Count; index++)
            {
                LineSegment2D segment = _segments[index];
                double localParameter = LineParameterService.GetParameter(
                    segment,
                    point,
                    tolerance);

                if (!LineIntersectionService.IsParameterOnSegment(
                        localParameter,
                        tolerance))
                {
                    continue;
                }

                double clamped = Math.Clamp(localParameter, 0.0, 1.0);
                Point2D projectedPoint = LineParameterService.PointAt(segment, clamped);
                double distance = projectedPoint.DistanceTo(point);

                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = index;
                bestLocalParameter = clamped;
                bestPoint = projectedPoint;
            }

            if (bestIndex < 0)
            {
                cut = default;
                return false;
            }

            double parameter = _segmentStarts[bestIndex] +
                               bestLocalParameter * _segments[bestIndex].Length;

            if (_polyline.IsClosed && Math.Abs(parameter - _totalLength) <= tolerance.Parameter)
            {
                parameter = 0.0;
                bestPoint = _polyline.Vertices[0];
            }

            cut = new CurveCut(parameter, bestPoint);
            return true;
        }

        public IReadOnlyList<CadEntity> BuildFragments(
            IReadOnlyList<CurveInterval> intervalsToKeep,
            GeometryTolerance tolerance)
        {
            var result = new List<CadEntity>();

            foreach (CurveInterval interval in intervalsToKeep)
            {
                if (interval.End.Parameter - interval.Start.Parameter <= tolerance.Parameter)
                {
                    continue;
                }

                List<Point2D> vertices = BuildVerticesForInterval(
                    interval.Start,
                    interval.End,
                    tolerance);

                AddPolylineIfValid(
                    result,
                    vertices,
                    tolerance);
            }

            return result;
        }

        private List<Point2D> BuildVerticesForInterval(
            CurveCut start,
            CurveCut end,
            GeometryTolerance tolerance)
        {
            var vertices = new List<Point2D>();
            AddIfDifferent(vertices, start.Point, tolerance);

            double startParameter = start.Parameter;
            double endParameter = end.Parameter;

            if (_polyline.IsClosed && endParameter <= startParameter)
            {
                endParameter += _totalLength;
            }

            foreach ((double parameter, Point2D vertex) in EnumerateForwardVertices(
                         startParameter,
                         endParameter,
                         tolerance))
            {
                AddIfDifferent(vertices, vertex, tolerance);
            }

            AddIfDifferent(vertices, end.Point, tolerance);
            return vertices;
        }

        private IEnumerable<(double Parameter, Point2D Vertex)> EnumerateForwardVertices(
            double startParameter,
            double endParameter,
            GeometryTolerance tolerance)
        {
            int vertexCount = _polyline.Vertices.Count;
            int maxPasses = _polyline.IsClosed ? 2 : 1;

            for (int pass = 0; pass < maxPasses; pass++)
            {
                double offset = pass * _totalLength;

                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    double vertexParameter = GetVertexParameter(vertexIndex) + offset;

                    if (vertexParameter <= startParameter + tolerance.Parameter ||
                        vertexParameter >= endParameter - tolerance.Parameter)
                    {
                        continue;
                    }

                    yield return (vertexParameter, _polyline.Vertices[vertexIndex]);
                }
            }
        }

        private double GetVertexParameter(int vertexIndex)
        {
            if (vertexIndex <= 0)
            {
                return 0.0;
            }

            if (vertexIndex >= _segmentStarts.Length)
            {
                return _totalLength;
            }

            return _segmentStarts[vertexIndex];
        }

        private int FindSegmentIndex(double parameter)
        {
            if (parameter >= _totalLength)
            {
                return _segments.Count - 1;
            }

            for (int index = _segments.Count - 1; index >= 0; index--)
            {
                if (parameter >= _segmentStarts[index])
                {
                    return index;
                }
            }

            return 0;
        }

        private void AddPolylineIfValid(
            ICollection<CadEntity> result,
            IReadOnlyList<Point2D> vertices,
            GeometryTolerance tolerance)
        {
            List<Point2D> cleanedVertices = RemoveConsecutiveDuplicates(vertices, tolerance);

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

            result.Add(new PolylineEntity(
                cleanedVertices,
                isClosed: false,
                layerId: _polyline.LayerId,
                style: _polyline.Style,
                isVisible: _polyline.IsVisible,
                isLocked: _polyline.IsLocked,
                drawOrder: _polyline.DrawOrder));
        }

        private static List<Point2D> RemoveConsecutiveDuplicates(
            IReadOnlyList<Point2D> vertices,
            GeometryTolerance tolerance)
        {
            var result = new List<Point2D>();

            foreach (Point2D vertex in vertices)
            {
                AddIfDifferent(result, vertex, tolerance);
            }

            return result;
        }

        private static void AddIfDifferent(
            ICollection<Point2D> vertices,
            Point2D point,
            GeometryTolerance tolerance)
        {
            if (vertices.Count > 0 && vertices.Last().DistanceTo(point) <= tolerance.Distance)
            {
                return;
            }

            vertices.Add(point);
        }
    }


    private static double GetEllipticalArcParameter(
        double startParameter,
        double endParameter,
        double valueParameter,
        bool isCounterClockwise)
    {
        double sweep = GetDirectedParameterDistance(
            startParameter,
            endParameter,
            isCounterClockwise);
        double value = GetDirectedParameterDistance(
            startParameter,
            valueParameter,
            isCounterClockwise);

        if (sweep <= 0.0)
        {
            return 0.0;
        }

        return value / sweep;
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
            return delta < 0.0
                ? delta + Math.PI * 2.0
                : delta;
        }

        double clockwiseDelta = start - end;
        return clockwiseDelta < 0.0
            ? clockwiseDelta + Math.PI * 2.0
            : clockwiseDelta;
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
                ? delta + Math.PI * 2.0
                : delta;
        }

        double clockwiseDelta = start - end;
        return clockwiseDelta < 0.0
            ? clockwiseDelta + Math.PI * 2.0
            : clockwiseDelta;
    }


    private static double DistanceSquared(
        Point2D first,
        Point2D second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;

        return (dx * dx) + (dy * dy);
    }

    private static double NormalizeRadians(double radians)
    {
        double value = radians % (Math.PI * 2.0);
        return value < 0.0 ? value + Math.PI * 2.0 : value;
    }

    private static double NormalizePeriodic(double parameter, double period)
    {
        if (period <= 0.0)
        {
            return parameter;
        }

        double value = parameter % period;
        return value < 0.0 ? value + period : value;
    }
}
