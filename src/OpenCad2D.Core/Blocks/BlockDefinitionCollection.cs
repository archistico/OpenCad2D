using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Blocks;

/// <summary>
/// Mutable collection of reusable block definitions owned by a CAD document.
/// </summary>
public sealed class BlockDefinitionCollection
{
    private readonly Dictionary<BlockDefinitionId, BlockDefinition> _definitions = new();

    public IReadOnlyList<BlockDefinition> All => _definitions.Values
        .OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public int Count => _definitions.Count;

    public bool Contains(BlockDefinitionId id)
    {
        return _definitions.ContainsKey(id);
    }

    public bool ContainsName(string name)
    {
        return _definitions.Values.Any(definition =>
            string.Equals(definition.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void Add(BlockDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_definitions.ContainsKey(definition.Id))
        {
            throw new InvalidOperationException(
                $"A block definition with id '{definition.Id}' already exists.");
        }

        if (ContainsName(definition.Name))
        {
            throw new InvalidOperationException(
                $"A block definition named '{definition.Name}' already exists.");
        }

        _definitions.Add(definition.Id, definition);
    }

    public void Replace(BlockDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_definitions.ContainsKey(definition.Id))
        {
            throw new InvalidOperationException(
                $"Cannot replace block definition '{definition.Id}' because it does not exist.");
        }

        BlockDefinition? conflictingName = _definitions.Values.FirstOrDefault(existing =>
            existing.Id != definition.Id &&
            string.Equals(existing.Name, definition.Name, StringComparison.OrdinalIgnoreCase));

        if (conflictingName is not null)
        {
            throw new InvalidOperationException(
                $"A block definition named '{definition.Name}' already exists.");
        }

        _definitions[definition.Id] = definition;
    }

    public bool TryGet(BlockDefinitionId id, out BlockDefinition? definition)
    {
        return _definitions.TryGetValue(id, out definition);
    }

    public BlockDefinition GetRequired(BlockDefinitionId id)
    {
        if (!_definitions.TryGetValue(id, out BlockDefinition? definition))
        {
            throw new InvalidOperationException(
                $"Block definition '{id}' does not exist.");
        }

        return definition;
    }

    public void RemoveRequired(BlockDefinitionId id)
    {
        if (!_definitions.Remove(id))
        {
            throw new InvalidOperationException(
                $"Cannot remove block definition '{id}' because it does not exist.");
        }
    }

    public void ReplaceAll(IEnumerable<BlockDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        _definitions.Clear();

        foreach (BlockDefinition definition in definitions)
        {
            Add(definition);
        }
    }
}
