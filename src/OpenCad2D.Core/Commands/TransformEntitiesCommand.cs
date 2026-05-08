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
        _oldEntities = document.Entities.GetByIds(_entityIds).ToList();

        _newEntities = _oldEntities
            .Select(entity => entity.Transform(_matrix))
            .ToList();

        foreach (CadEntity entity in _newEntities)
        {
            document.ReplaceEntity(entity);
        }
    }

    public void Undo(CadDocument document)
    {
        if (_oldEntities is null)
        {
            throw new InvalidOperationException(
                "Cannot undo transform before execute.");
        }

        foreach (CadEntity entity in _oldEntities)
        {
            document.ReplaceEntity(entity);
        }
    }
}