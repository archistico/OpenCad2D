using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces existing entities with new versions that have the same ids.
/// </summary>
public sealed class ReplaceEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<CadEntity> _newEntities;
    private List<CadEntity>? _oldEntities;

    public ReplaceEntitiesCommand(CadEntity entity)
        : this(new[] { entity })
    {
    }

    public ReplaceEntitiesCommand(IEnumerable<CadEntity> newEntities)
    {
        ArgumentNullException.ThrowIfNull(newEntities);

        _newEntities = newEntities.ToList();

        if (_newEntities.Count == 0)
        {
            throw new ArgumentException(
                "At least one entity is required.",
                nameof(newEntities));
        }
    }

    public string Name => "Replace entities";

    public void Execute(CadDocument document)
    {
        _oldEntities = document.Entities
            .GetByIds(_newEntities.Select(entity => entity.Id))
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
                "Cannot undo replace before execute.");
        }

        foreach (CadEntity entity in _oldEntities)
        {
            document.ReplaceEntity(entity);
        }
    }
}