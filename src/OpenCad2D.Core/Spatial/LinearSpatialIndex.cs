using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Spatial;

/// <summary>
/// Simple spatial index implementation that scans stored bounding boxes linearly.
/// Useful as a baseline and as a drop-in implementation before introducing
/// Quadtree or R-Tree indexing.
/// </summary>
public sealed class LinearSpatialIndex : ISpatialIndex
{
    private readonly Dictionary<EntityId, SpatialIndexEntry> _entries = new();

    public void Add(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entries.ContainsKey(entity.Id))
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id}' is already indexed.");
        }

        _entries.Add(
            entity.Id,
            new SpatialIndexEntry(
                entity,
                entity.GetBoundingBox()));
    }

    public bool Remove(EntityId id)
    {
        return _entries.Remove(id);
    }

    public void Replace(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entries.ContainsKey(entity.Id))
        {
            throw new KeyNotFoundException(
                $"Entity '{entity.Id}' was not found in the spatial index.");
        }

        _entries[entity.Id] = new SpatialIndexEntry(
            entity,
            entity.GetBoundingBox());
    }

    public void Clear()
    {
        _entries.Clear();
    }

    public IReadOnlyList<CadEntity> Query(BoundingBox2D area)
    {
        return _entries.Values
            .Where(entry => entry.Bounds.Intersects(area))
            .Select(entry => entry.Entity)
            .ToList();
    }

    private sealed record SpatialIndexEntry(
        CadEntity Entity,
        BoundingBox2D Bounds);
}