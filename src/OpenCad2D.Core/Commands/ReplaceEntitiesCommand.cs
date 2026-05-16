using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces existing entities with new versions that have the same ids.
/// </summary>
public sealed class ReplaceEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<CadEntity> _newEntities;
    private readonly bool _markDimensionsStale;
    private List<CadEntity>? _oldEntities;
    private List<DimensionEntity>? _oldDimensions;

    public ReplaceEntitiesCommand(CadEntity entity, bool markDimensionsStale = false)
        : this(new[] { entity }, markDimensionsStale)
    {
    }

    public ReplaceEntitiesCommand(
        IEnumerable<CadEntity> newEntities,
        bool markDimensionsStale = false)
    {
        ArgumentNullException.ThrowIfNull(newEntities);

        _markDimensionsStale = markDimensionsStale;

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
        ArgumentNullException.ThrowIfNull(document);

        _oldEntities = document.Entities
            .GetByIds(_newEntities.Select(entity => entity.Id))
            .ToList();
        _oldDimensions = _markDimensionsStale
            ? DimensionStaleStateHelper.Capture(document)
            : null;

        document.ReplaceEntities(_newEntities);

        if (_markDimensionsStale)
        {
            DimensionStaleStateHelper.MarkAllStale(document);
        }
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_oldEntities is null)
        {
            throw new InvalidOperationException(
                "Cannot undo replace before execute.");
        }

        document.ReplaceEntities(_oldEntities);

        if (_markDimensionsStale)
        {
            DimensionStaleStateHelper.Restore(document, _oldDimensions);
        }
    }
}