using OpenCad2D.Core.Collections;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Documents;

/// <summary>
/// Represents an in-memory CAD document.
/// </summary>
public sealed class CadDocument
{
    public CadDocument()
    {
        Layers = new LayerCollection();
        Entities = new EntityCollection();
    }

    public LayerCollection Layers { get; }

    public EntityCollection Entities { get; }

    public void AddEntity(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Layers.Contains(entity.LayerId))
        {
            throw new InvalidOperationException(
                $"Cannot add entity because layer '{entity.LayerId}' does not exist.");
        }

        Entities.Add(entity);
    }

    public void AddEntities(IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (CadEntity entity in entities)
        {
            AddEntity(entity);
        }
    }

    public void ReplaceEntity(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Layers.Contains(entity.LayerId))
        {
            throw new InvalidOperationException(
                $"Cannot replace entity because layer '{entity.LayerId}' does not exist.");
        }

        Entities.Replace(entity);
    }

    public bool IsEntityVisible(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!entity.IsVisible)
        {
            return false;
        }

        Layer layer = Layers.GetRequired(entity.LayerId);

        return layer.IsVisible;
    }

    public IEnumerable<CadEntity> GetVisibleEntities()
    {
        return Entities.All.Where(IsEntityVisible);
    }

    public IEnumerable<CadEntity> GetVisibleEntities(BoundingBox2D area)
    {
        return Entities.Query(area).Where(IsEntityVisible);
    }

    public void ReplaceEntities(IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (CadEntity entity in entities)
        {
            ReplaceEntity(entity);
        }
    }

    public void RemoveEntity(EntityId id)
    {
        Entities.RemoveRequired(id);
    }

    public void RemoveEntities(IEnumerable<EntityId> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        foreach (EntityId id in ids.ToList())
        {
            RemoveEntity(id);
        }
    }
}