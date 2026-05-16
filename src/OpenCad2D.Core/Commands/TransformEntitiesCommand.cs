using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Transforms existing entities using a 2D matrix.
/// </summary>
public class TransformEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<EntityId> _entityIds;
    private readonly Matrix2D _matrix;

    private List<CadEntity>? _oldEntities;
    private List<CadEntity>? _newEntities;
    private List<DimensionEntity>? _oldDimensions;

    public TransformEntitiesCommand(
        IEnumerable<EntityId> entityIds,
        Matrix2D matrix)
    {
        ArgumentNullException.ThrowIfNull(entityIds);

        _entityIds = entityIds.ToList();

        if (_entityIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one entity id is required.",
                nameof(entityIds));
        }

        _matrix = matrix;
    }

    public virtual string Name => "Transform entities";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _oldEntities = document.Entities
            .GetByIds(_entityIds)
            .ToList();
        _oldDimensions = DimensionStaleStateHelper.Capture(document);

        _newEntities = _oldEntities
            .Select(entity => entity.Transform(_matrix))
            .ToList();

        document.ReplaceEntities(_newEntities);
        DimensionStaleStateHelper.MarkAllStale(document);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_oldEntities is null)
        {
            throw new InvalidOperationException(
                "Cannot undo transform before execute.");
        }

        document.ReplaceEntities(_oldEntities);
        DimensionStaleStateHelper.Restore(document, _oldDimensions);
    }
}