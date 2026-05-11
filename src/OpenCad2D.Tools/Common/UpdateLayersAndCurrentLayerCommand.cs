using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Updates the document layer collection and the workspace current layer as a
/// single undoable operation.
/// </summary>
internal sealed class UpdateLayersAndCurrentLayerCommand : ICadCommand
{
    private readonly IReadOnlyList<Layer> _oldLayers;
    private readonly IReadOnlyList<Layer> _newLayers;
    private readonly LayerId _oldCurrentLayerId;
    private readonly LayerId _newCurrentLayerId;
    private readonly Action<LayerId> _setCurrentLayerId;

    public UpdateLayersAndCurrentLayerCommand(
        IEnumerable<Layer> oldLayers,
        IEnumerable<Layer> newLayers,
        LayerId oldCurrentLayerId,
        LayerId newCurrentLayerId,
        Action<LayerId> setCurrentLayerId)
    {
        ArgumentNullException.ThrowIfNull(oldLayers);
        ArgumentNullException.ThrowIfNull(newLayers);
        ArgumentNullException.ThrowIfNull(setCurrentLayerId);

        _oldLayers = oldLayers.ToList();
        _newLayers = newLayers.ToList();
        _oldCurrentLayerId = oldCurrentLayerId;
        _newCurrentLayerId = newCurrentLayerId;
        _setCurrentLayerId = setCurrentLayerId;
    }

    public string Name => "Update Layers";

    public void Execute(CadDocument document)
    {
        Apply(
            document,
            _newLayers,
            _newCurrentLayerId);
    }

    public void Undo(CadDocument document)
    {
        Apply(
            document,
            _oldLayers,
            _oldCurrentLayerId);
    }

    private void Apply(
        CadDocument document,
        IReadOnlyList<Layer> layers,
        LayerId currentLayerId)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateReferencedLayersExist(
            document,
            layers);

        ValidateCurrentLayerExists(
            layers,
            currentLayerId);

        document.Layers.ReplaceAll(layers);
        _setCurrentLayerId(currentLayerId);
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

    private static void ValidateCurrentLayerExists(
        IReadOnlyList<Layer> layers,
        LayerId currentLayerId)
    {
        if (layers.Any(layer => layer.Id == currentLayerId))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cannot update layers because current layer '{currentLayerId}' does not exist.");
    }
}
