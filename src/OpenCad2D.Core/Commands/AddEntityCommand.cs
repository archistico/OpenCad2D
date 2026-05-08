using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Adds one or more entities to a CAD document.
/// </summary>
public sealed class AddEntityCommand : ICadCommand
{
    private readonly IReadOnlyList<CadEntity> _entities;

    public AddEntityCommand(CadEntity entity)
        : this(new[] { entity })
    {
    }

    public AddEntityCommand(IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities.ToList();

        if (_entities.Count == 0)
        {
            throw new ArgumentException(
                "At least one entity is required.",
                nameof(entities));
        }
    }

    public string Name => "Add entity";

    public void Execute(CadDocument document)
    {
        document.AddEntities(_entities);
    }

    public void Undo(CadDocument document)
    {
        document.Entities.RemoveMany(_entities.Select(entity => entity.Id));
    }
}