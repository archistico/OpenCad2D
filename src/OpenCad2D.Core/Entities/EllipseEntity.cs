using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing a full ellipse using a center, a major axis vector and a minor radius.
/// </summary>
public sealed class EllipseEntity : CadEntity
{
    public const int DefaultSampleCount = 96;

    public EllipseEntity(
        Point2D center,
        Vector2D majorAxis,
        double minorRadius,
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
                "Ellipse major axis length must be greater than zero.");
        }

        if (minorRadius <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minorRadius),
                "Ellipse minor radius must be greater than zero.");
        }

        Center = center;
        MajorAxis = majorAxis;
        MinorRadius = minorRadius;
    }

    public Point2D Center { get; }

    public Vector2D MajorAxis { get; }

    public double MajorRadius => MajorAxis.Length;

    public double MinorRadius { get; }

    public Vector2D MajorDirection => MajorAxis.Normalize();

    public Vector2D MinorAxis => MajorDirection.PerpendicularLeft() * MinorRadius;

    public double RotationRadians => Math.Atan2(MajorAxis.Y, MajorAxis.X);

    public double RotationDegrees => RotationRadians * 180.0 / Math.PI;

    public Point2D MajorAxisEndPoint => Center + MajorAxis;

    public Point2D MajorAxisStartPoint => Center - MajorAxis;

    public Point2D MinorAxisEndPoint => Center + MinorAxis;

    public Point2D MinorAxisStartPoint => Center - MinorAxis;

    public override EntityKind Kind => EntityKind.Ellipse;

    public override BoundingBox2D GetBoundingBox()
    {
        double cos = Math.Cos(RotationRadians);
        double sin = Math.Sin(RotationRadians);
        double halfWidth = Math.Sqrt(
            MajorRadius * MajorRadius * cos * cos +
            MinorRadius * MinorRadius * sin * sin);
        double halfHeight = Math.Sqrt(
            MajorRadius * MajorRadius * sin * sin +
            MinorRadius * MinorRadius * cos * cos);

        return new BoundingBox2D(
            Center.X - halfWidth,
            Center.Y - halfHeight,
            Center.X + halfWidth,
            Center.Y + halfHeight);
    }

    public override double DistanceTo(Point2D point)
    {
        return point.DistanceTo(GetClosestPoint(point));
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        Point2D closest = GetPointAt(0);
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
        if (sampleCount < 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleCount),
                "Ellipse sample count must be at least 8.");
        }

        var points = new List<Point2D>(sampleCount);
        for (int i = 0; i < sampleCount; i++)
        {
            double angle = i * Math.Tau / sampleCount;
            points.Add(GetPointAt(angle));
        }

        return points;
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new EllipseEntity(
            matrix.Transform(Center),
            matrix.Transform(MajorAxis),
            matrix.Transform(MinorAxis).Length,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new EllipseEntity(
            Center,
            MajorAxis,
            MinorRadius,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new EllipseEntity(
            Center,
            MajorAxis,
            MinorRadius,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }
}
