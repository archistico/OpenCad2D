using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing a native elliptical arc using the same parametric
/// definition as <see cref="EllipseEntity"/>, plus start/end parameters.
/// </summary>
public sealed class EllipticalArcEntity : CadEntity
{
    public const int DefaultSampleCount = 64;

    public EllipticalArcEntity(
        Point2D center,
        Vector2D majorAxis,
        double minorRadius,
        double startParameterRadians,
        double endParameterRadians,
        bool isCounterClockwise = true,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        if (majorAxis.Length <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorAxis),
                "Elliptical arc major axis length must be greater than zero.");
        }

        if (minorRadius <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minorRadius),
                "Elliptical arc minor radius must be greater than zero.");
        }

        Center = center;
        MajorAxis = majorAxis;
        MinorRadius = minorRadius;
        StartParameterRadians = NormalizeRadians(startParameterRadians);
        EndParameterRadians = NormalizeRadians(endParameterRadians);
        IsCounterClockwise = isCounterClockwise;
    }

    public Point2D Center { get; }

    public Vector2D MajorAxis { get; }

    public double MajorRadius => MajorAxis.Length;

    public double MinorRadius { get; }

    public double StartParameterRadians { get; }

    public double EndParameterRadians { get; }

    public bool IsCounterClockwise { get; }

    public Vector2D MajorDirection => MajorAxis.Normalize();

    public Vector2D MinorAxis => MajorDirection.PerpendicularLeft() * MinorRadius;

    public double RotationRadians => Math.Atan2(MajorAxis.Y, MajorAxis.X);

    public double SweepRadians => GetDirectedParameterDistance(
        StartParameterRadians,
        EndParameterRadians,
        IsCounterClockwise);

    public Point2D StartPoint => GetPointAt(StartParameterRadians);

    public Point2D EndPoint => GetPointAt(EndParameterRadians);

    public override EntityKind Kind => EntityKind.EllipticalArc;

    public override BoundingBox2D GetBoundingBox()
    {
        IReadOnlyList<Point2D> points = GetSamplePoints();
        double minX = points.Min(point => point.X);
        double minY = points.Min(point => point.Y);
        double maxX = points.Max(point => point.X);
        double maxY = points.Max(point => point.Y);

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public override double DistanceTo(Point2D point)
    {
        return point.DistanceTo(GetClosestPoint(point));
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        Point2D closest = StartPoint;
        double closestDistance = point.DistanceTo(closest);

        foreach (Point2D candidate in GetSamplePoints(DefaultSampleCount).Skip(1))
        {
            double distance = point.DistanceTo(candidate);
            if (distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public Point2D GetPointAt(double parameterRadians)
    {
        Vector2D minorAxis = MinorAxis;
        return Center +
            MajorAxis * Math.Cos(parameterRadians) +
            minorAxis * Math.Sin(parameterRadians);
    }

    public IReadOnlyList<Point2D> GetSamplePoints(int sampleCount = DefaultSampleCount)
    {
        if (sampleCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleCount),
                "Elliptical arc sample count must be at least 2.");
        }

        var points = new List<Point2D>(sampleCount + 1);
        double signedSweep = IsCounterClockwise
            ? SweepRadians
            : -SweepRadians;

        for (int index = 0; index <= sampleCount; index++)
        {
            double parameter = StartParameterRadians + signedSweep * index / sampleCount;
            points.Add(GetPointAt(parameter));
        }

        return points;
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        Point2D transformedCenter = matrix.Transform(Center);
        Vector2D transformedMajorAxis = matrix.Transform(MajorAxis);
        double transformedMinorRadius = matrix.Transform(MinorAxis).Length;
        bool transformedCounterClockwise = HasNegativeDeterminant(matrix)
            ? !IsCounterClockwise
            : IsCounterClockwise;

        return new EllipticalArcEntity(
            transformedCenter,
            transformedMajorAxis,
            transformedMinorRadius,
            StartParameterRadians,
            EndParameterRadians,
            transformedCounterClockwise,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new EllipticalArcEntity(
            Center,
            MajorAxis,
            MinorRadius,
            StartParameterRadians,
            EndParameterRadians,
            IsCounterClockwise,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new EllipticalArcEntity(
            Center,
            MajorAxis,
            MinorRadius,
            StartParameterRadians,
            EndParameterRadians,
            IsCounterClockwise,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    private static bool HasNegativeDeterminant(Matrix2D matrix)
    {
        double determinant = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21);

        return determinant < 0;
    }

    private static double GetDirectedParameterDistance(
        double start,
        double end,
        bool isCounterClockwise)
    {
        double normalizedStart = NormalizeRadians(start);
        double normalizedEnd = NormalizeRadians(end);

        if (isCounterClockwise)
        {
            return normalizedEnd >= normalizedStart
                ? normalizedEnd - normalizedStart
                : normalizedEnd + Math.Tau - normalizedStart;
        }

        return normalizedStart >= normalizedEnd
            ? normalizedStart - normalizedEnd
            : normalizedStart + Math.Tau - normalizedEnd;
    }

    private static double NormalizeRadians(double radians)
    {
        double value = radians % Math.Tau;
        return value < 0.0 ? value + Math.Tau : value;
    }
}
