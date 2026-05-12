using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Non-associative diameter dimension defined by a center point, a point on the circle and a text placement point.
/// </summary>
public sealed class DiameterDimensionEntity : DimensionEntity
{
    public DiameterDimensionEntity(
        Point2D center,
        Point2D pointOnCircle,
        Point2D textPoint,
        DimensionStyleId? dimensionStyleId = null,
        string? textOverride = null,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0)
        : base(
            dimensionStyleId,
            textOverride,
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        if (center.DistanceTo(pointOnCircle) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Diameter dimension requires a point on the circle different from the center.",
                nameof(pointOnCircle));
        }

        Center = center;
        PointOnCircle = pointOnCircle;
        TextPoint = textPoint;
    }

    public Point2D Center { get; }

    public Point2D PointOnCircle { get; }

    public Point2D TextPoint { get; }

    public Point2D OppositePoint
    {
        get
        {
            Vector2D radius = PointOnCircle - Center;
            return Center - radius;
        }
    }

    public override double MeasurementValue => Center.DistanceTo(PointOnCircle) * 2.0;

    public override EntityKind Kind => EntityKind.DiameterDimension;

    public override BoundingBox2D GetBoundingBox()
    {
        Point2D opposite = OppositePoint;
        double minX = Math.Min(Math.Min(Math.Min(Center.X, PointOnCircle.X), opposite.X), TextPoint.X);
        double minY = Math.Min(Math.Min(Math.Min(Center.Y, PointOnCircle.Y), opposite.Y), TextPoint.Y);
        double maxX = Math.Max(Math.Max(Math.Max(Center.X, PointOnCircle.X), opposite.X), TextPoint.X);
        double maxY = Math.Max(Math.Max(Math.Max(Center.Y, PointOnCircle.Y), opposite.Y), TextPoint.Y);

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToSegment(
            point,
            new LineSegment2D(OppositePoint, PointOnCircle));
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnSegment(
            point,
            new LineSegment2D(OppositePoint, PointOnCircle));
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new DiameterDimensionEntity(
            matrix.Transform(Center),
            matrix.Transform(PointOnCircle),
            matrix.Transform(TextPoint),
            DimensionStyleId,
            TextOverride,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new DiameterDimensionEntity(
            Center,
            PointOnCircle,
            TextPoint,
            DimensionStyleId,
            TextOverride,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new DiameterDimensionEntity(
            Center,
            PointOnCircle,
            TextPoint,
            DimensionStyleId,
            TextOverride,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }
}
