using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces a set of existing entities with a different set of new entities.
/// </summary>
/// <remarks>
/// This command is intended for topological modify operations where one entity
/// can become zero, one or many entities, such as break, trim, explode and
/// future fillet/chamfer operations.
/// </remarks>
public sealed class ModifyEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<CadEntity> _removedEntities;
    private readonly IReadOnlyList<CadEntity> _addedEntities;
    private List<DimensionEntity>? _oldDimensions;

    public ModifyEntitiesCommand(
        IEnumerable<CadEntity> removedEntities,
        IEnumerable<CadEntity> addedEntities,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(removedEntities);
        ArgumentNullException.ThrowIfNull(addedEntities);

        _removedEntities = removedEntities.ToList();
        _addedEntities = addedEntities.ToList();

        if (_removedEntities.Count == 0 && _addedEntities.Count == 0)
        {
            throw new ArgumentException(
                "At least one removed or added entity is required.",
                nameof(removedEntities));
        }

        Name = string.IsNullOrWhiteSpace(name)
            ? "Modify entities"
            : name;
    }

    public string Name { get; }

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _oldDimensions = DimensionStaleStateHelper.Capture(document);

        if (_removedEntities.Count > 0)
        {
            document.RemoveEntities(_removedEntities.Select(entity => entity.Id));
        }

        if (_addedEntities.Count > 0)
        {
            document.AddEntities(_addedEntities);
        }

        DimensionStaleStateHelper.MarkAllStale(document);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_addedEntities.Count > 0)
        {
            document.RemoveEntities(_addedEntities.Select(entity => entity.Id));
        }

        if (_removedEntities.Count > 0)
        {
            document.AddEntities(_removedEntities);
        }

        DimensionStaleStateHelper.Restore(document, _oldDimensions);
    }
}
