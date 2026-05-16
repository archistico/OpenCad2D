using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Represents an entity picked from the drawing together with the model-space point used to pick it.
/// Several CAD modify commands need both values because the picked side determines the geometric result.
/// </summary>
public sealed class ToolPickedEntityInput
{
    public ToolPickedEntityInput(
        EntityId entityId,
        Point2D pickPoint,
        Point2D closestPoint,
        CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        EntityId = entityId;
        PickPoint = pickPoint;
        ClosestPoint = closestPoint;
        Entity = entity;
    }

    public EntityId EntityId { get; }

    public Point2D PickPoint { get; }

    public Point2D ClosestPoint { get; }

    public CadEntity Entity { get; }
}
