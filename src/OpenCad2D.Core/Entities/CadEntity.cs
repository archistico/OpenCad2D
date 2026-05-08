using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Base class for all persistent CAD entities.
/// </summary>
public abstract class CadEntity
{
    protected CadEntity(
        EntityId id,
        LayerId layerId,
        EntityStyle style,
        bool isVisible,
        bool isLocked,
        int drawOrder)
    {
        Id = id;
        LayerId = layerId;
        Style = style;
        IsVisible = isVisible;
        IsLocked = isLocked;
        DrawOrder = drawOrder;
    }

    public EntityId Id { get; }

    public LayerId LayerId { get; }

    public EntityStyle Style { get; }

    public bool IsVisible { get; }

    public bool IsLocked { get; }

    public int DrawOrder { get; }

    public abstract EntityKind Kind { get; }

    public abstract BoundingBox2D GetBoundingBox();

    public abstract double DistanceTo(Point2D point);

    public abstract Point2D GetClosestPoint(Point2D point);

    public abstract CadEntity Transform(Matrix2D matrix);

    public abstract CadEntity WithId(EntityId id);

    protected T CopyCommonTo<T>(T entity)
        where T : CadEntity
    {
        return entity;
    }
}