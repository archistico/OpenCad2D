using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Collections;

/// <summary>
/// Collection of CAD entities indexed by EntityId.
/// </summary>
public sealed class EntityCollection
{
    private readonly Dictionary<EntityId, CadEntity> _entities = new();

    public IReadOnlyCollection<CadEntity> All => _entities.Values;

    public int Count => _entities.Count;

    public void Add(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.ContainsKey(entity.Id))
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id}' already exists.");
        }

        _entities.Add(entity.Id, entity);
    }

    public void AddRange(IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (CadEntity entity in entities)
        {
            Add(entity);
        }
    }

    public bool Contains(EntityId id)
    {
        return _entities.ContainsKey(id);
    }

    public CadEntity GetRequired(EntityId id)
    {
        if (!_entities.TryGetValue(id, out CadEntity? entity))
        {
            throw new KeyNotFoundException(
                $"Entity '{id}' was not found.");
        }

        return entity;
    }

    public bool TryGet(EntityId id, out CadEntity? entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    public IReadOnlyList<CadEntity> GetByIds(IEnumerable<EntityId> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var result = new List<CadEntity>();

        foreach (EntityId id in ids)
        {
            result.Add(GetRequired(id));
        }

        return result;
    }

    public bool Remove(EntityId id)
    {
        return _entities.Remove(id);
    }

    public void RemoveRequired(EntityId id)
    {
        if (!_entities.Remove(id))
        {
            throw new KeyNotFoundException(
                $"Entity '{id}' was not found.");
        }
    }

    public void RemoveMany(IEnumerable<EntityId> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        foreach (EntityId id in ids)
        {
            RemoveRequired(id);
        }
    }

    public void Replace(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entities.ContainsKey(entity.Id))
        {
            throw new KeyNotFoundException(
                $"Entity '{entity.Id}' was not found.");
        }

        _entities[entity.Id] = entity;
    }

    public void ReplaceMany(IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (CadEntity entity in entities)
        {
            Replace(entity);
        }
    }

    public void Clear()
    {
        _entities.Clear();
    }
}