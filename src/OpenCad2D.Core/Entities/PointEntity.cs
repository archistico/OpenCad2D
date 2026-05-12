using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing a precise point in model space.
/// </summary>
public sealed class PointEntity : CadEntity
{
    public PointEntity(
        Point2D position,
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
        Position = position;
    }

    /// <summary>
    /// Gets the exact model-space position of the point.
    /// </summary>
    public Point2D Position { get; }

    public override EntityKind Kind => EntityKind.Point;

    public override BoundingBox2D GetBoundingBox()
    {
        return new BoundingBox2D(
            Position.X,
            Position.Y,
            Position.X,
            Position.Y);
    }

    public override double DistanceTo(Point2D point)
    {
        return Position.DistanceTo(point);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return Position;
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new PointEntity(
            matrix.Transform(Position),
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new PointEntity(
            Position,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new PointEntity(
            Position,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }
}
