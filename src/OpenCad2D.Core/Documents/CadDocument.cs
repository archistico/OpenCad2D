using OpenCad2D.Core.Collections;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Layers;

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
}