using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces the document layer collection as a single undoable operation.
/// </summary>
public sealed class UpdateLayersCommand : ICadCommand
{
    private readonly IReadOnlyList<Layer> _oldLayers;
    private readonly IReadOnlyList<Layer> _newLayers;

    public UpdateLayersCommand(
        IEnumerable<Layer> oldLayers,
        IEnumerable<Layer> newLayers)
    {
        ArgumentNullException.ThrowIfNull(oldLayers);
        ArgumentNullException.ThrowIfNull(newLayers);

        _oldLayers = oldLayers.ToList();
        _newLayers = newLayers.ToList();
    }

    public string Name => "Update Layers";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateReferencedLayersExist(
            document,
            _newLayers);

        document.Layers.ReplaceAll(_newLayers);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateReferencedLayersExist(
            document,
            _oldLayers);

        document.Layers.ReplaceAll(_oldLayers);
    }

    private static void ValidateReferencedLayersExist(
        CadDocument document,
        IReadOnlyList<Layer> layers)
    {
        HashSet<LayerId> availableLayerIds = layers
            .Select(layer => layer.Id)
            .ToHashSet();

        foreach (LayerId layerId in document.Entities.All.Select(entity => entity.LayerId).Distinct())
        {
            if (!availableLayerIds.Contains(layerId))
            {
                throw new InvalidOperationException(
                    $"Cannot update layers because layer '{layerId}' is still used by one or more entities.");
            }
        }
    }
}
