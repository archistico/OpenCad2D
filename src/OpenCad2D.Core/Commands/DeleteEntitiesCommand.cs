using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Deletes entities from a CAD document.
/// </summary>
public sealed class DeleteEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<EntityId> _entityIds;
    private List<CadEntity>? _deletedEntities;
    private List<DimensionEntity>? _oldDimensions;

    public DeleteEntitiesCommand(IEnumerable<EntityId> entityIds)
    {
        ArgumentNullException.ThrowIfNull(entityIds);

        _entityIds = entityIds.ToList();

        if (_entityIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one entity id is required.",
                nameof(entityIds));
        }
    }

    public string Name => "Delete entities";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _oldDimensions = DimensionStaleStateHelper.Capture(document);
        _deletedEntities = document.Entities.GetByIds(_entityIds).ToList();
        bool deletesModelGeometry = _deletedEntities.Any(entity => entity is not DimensionEntity);

        document.RemoveEntities(_entityIds);

        if (deletesModelGeometry)
        {
            DimensionStaleStateHelper.MarkAllStale(document);
        }
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_deletedEntities is null)
        {
            return;
        }

        document.AddEntities(_deletedEntities);
        DimensionStaleStateHelper.Restore(document, _oldDimensions);
    }
}