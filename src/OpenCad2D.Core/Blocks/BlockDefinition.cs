using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Blocks;

/// <summary>
/// Reusable block definition made of regular CAD entities in local block coordinates.
/// </summary>
public sealed class BlockDefinition
{
    private readonly List<CadEntity> _entities;

    public BlockDefinition(
        BlockDefinitionId id,
        string name,
        IEnumerable<CadEntity> entities)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Block definition name cannot be empty.",
                nameof(name));
        }

        ArgumentNullException.ThrowIfNull(entities);

        Id = id;
        Name = name.Trim();
        _entities = entities.ToList();
    }

    public BlockDefinitionId Id { get; }

    public string Name { get; }

    public IReadOnlyList<CadEntity> Entities => _entities;

    public bool IsEmpty => _entities.Count == 0;

    public BoundingBox2D GetBoundingBox()
    {
        if (_entities.Count == 0)
        {
            return new BoundingBox2D(0, 0, 0, 0);
        }

        BoundingBox2D first = _entities[0].GetBoundingBox();
        double minX = first.MinX;
        double minY = first.MinY;
        double maxX = first.MaxX;
        double maxY = first.MaxY;

        foreach (CadEntity entity in _entities.Skip(1))
        {
            BoundingBox2D box = entity.GetBoundingBox();
            minX = Math.Min(minX, box.MinX);
            minY = Math.Min(minY, box.MinY);
            maxX = Math.Max(maxX, box.MaxX);
            maxY = Math.Max(maxY, box.MaxY);
        }

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public BlockDefinition WithName(string name)
    {
        return new BlockDefinition(
            Id,
            name,
            _entities);
    }

    public BlockDefinition WithEntities(IEnumerable<CadEntity> entities)
    {
        return new BlockDefinition(
            Id,
            Name,
            entities);
    }
}
