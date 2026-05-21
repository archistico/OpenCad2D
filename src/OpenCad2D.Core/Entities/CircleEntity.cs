using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing a circle.
/// </summary>
public sealed class CircleEntity : CadEntity, IFillableEntity
{
    public CircleEntity(
        Point2D center,
        double radius,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0,
        bool isFilled = false)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        Geometry = new Circle2D(center, radius);
        IsFilled = isFilled;
    }

    public Circle2D Geometry { get; }

    public Point2D Center => Geometry.Center;

    public double Radius => Geometry.Radius;

    public bool IsFilled { get; }

    public override EntityKind Kind => EntityKind.Circle;

    public override BoundingBox2D GetBoundingBox()
    {
        return Geometry.GetBoundingBox();
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToCircle(point, Geometry);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnCircle(point, Geometry);
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        Point2D transformedCenter = matrix.Transform(Center);
        Point2D transformedRadiusPoint = matrix.Transform(
            new Point2D(Center.X + Radius, Center.Y));

        double transformedRadius = transformedCenter.DistanceTo(transformedRadiusPoint);

        return new CircleEntity(
            transformedCenter,
            transformedRadius,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new CircleEntity(
            Center,
            Radius,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new CircleEntity(
            Center,
            Radius,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled);
    }

    public CadEntity WithFill(bool isFilled)
    {
        return new CircleEntity(
            Center,
            Radius,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            isFilled);
    }
}
