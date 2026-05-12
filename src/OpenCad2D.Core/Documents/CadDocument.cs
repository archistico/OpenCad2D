using OpenCad2D.Core.Collections;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
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
        LineFormats = LineFormatCollection.Default;
        TextFormats = TextFormatCollection.Default;
        DimensionStyles = DimensionStyleCollection.Default;
        Entities = new EntityCollection();
    }

    public LayerCollection Layers { get; }

    public LineFormatCollection LineFormats { get; }

    public TextFormatCollection TextFormats { get; }

    public DimensionStyleCollection DimensionStyles { get; }

    public EntityCollection Entities { get; }

    public void ReplaceLineFormats(LineFormatCollection lineFormats)
    {
        ArgumentNullException.ThrowIfNull(lineFormats);

        LineFormats.ReplaceAll(lineFormats.All);
    }

    public void ReplaceTextFormats(TextFormatCollection textFormats)
    {
        ArgumentNullException.ThrowIfNull(textFormats);

        TextFormats.ReplaceAll(textFormats.All);
    }

    public void ReplaceDimensionStyles(DimensionStyleCollection dimensionStyles)
    {
        ArgumentNullException.ThrowIfNull(dimensionStyles);

        DimensionStyles.ReplaceAll(dimensionStyles.All);
    }

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

        CadEntity existingEntity = Entities.GetRequired(entity.Id);

        EnsureEntityIsEditable(
            existingEntity,
            "replace");

        Entities.Replace(entity);
    }

    public void ReplaceEntities(IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (CadEntity entity in entities.ToList())
        {
            ReplaceEntity(entity);
        }
    }

    public void RemoveEntity(EntityId id)
    {
        CadEntity existingEntity = Entities.GetRequired(id);

        EnsureEntityIsEditable(
            existingEntity,
            "remove");

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

    public bool IsEntityOnLockedLayer(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Layer layer = Layers.GetRequired(entity.LayerId);

        return layer.IsLocked;
    }

    public bool IsEntitySelectable(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return IsEntityVisible(entity) &&
               !IsEntityOnLockedLayer(entity);
    }

    public IEnumerable<CadEntity> GetVisibleEntities()
    {
        return Entities.All.Where(IsEntityVisible);
    }

    public IEnumerable<CadEntity> GetVisibleEntities(BoundingBox2D area)
    {
        return Entities.Query(area).Where(IsEntityVisible);
    }

    public IEnumerable<CadEntity> GetSelectableEntities()
    {
        return Entities.All.Where(IsEntitySelectable);
    }

    public IEnumerable<CadEntity> GetSelectableEntities(BoundingBox2D area)
    {
        return Entities.Query(area).Where(IsEntitySelectable);
    }

    private void EnsureEntityIsEditable(
        CadEntity entity,
        string operationName)
    {
        if (!IsEntityOnLockedLayer(entity))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cannot {operationName} entity '{entity.Id}' because layer '{entity.LayerId}' is locked.");
    }
}