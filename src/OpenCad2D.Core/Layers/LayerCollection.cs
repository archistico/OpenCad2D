using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Layers;

/// <summary>
/// Collection of CAD layers.
/// </summary>
public sealed class LayerCollection
{
    private readonly Dictionary<LayerId, Layer> _layers = new();

    public LayerCollection()
    {
        Add(Layer.Default);
    }

    public IReadOnlyCollection<Layer> All => _layers.Values;

    public int Count => _layers.Count;

    public Layer Default => _layers[LayerId.Default];

    public void Add(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (_layers.ContainsKey(layer.Id))
        {
            throw new InvalidOperationException(
                $"Layer '{layer.Id}' already exists.");
        }

        _layers.Add(layer.Id, layer);
    }

    public bool Contains(LayerId id)
    {
        return _layers.ContainsKey(id);
    }

    public Layer GetRequired(LayerId id)
    {
        if (!_layers.TryGetValue(id, out Layer? layer))
        {
            throw new KeyNotFoundException(
                $"Layer '{id}' was not found.");
        }

        return layer;
    }

    public bool TryGet(LayerId id, out Layer? layer)
    {
        return _layers.TryGetValue(id, out layer);
    }

    public void Replace(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (!_layers.ContainsKey(layer.Id))
        {
            throw new KeyNotFoundException(
                $"Layer '{layer.Id}' was not found.");
        }

        _layers[layer.Id] = layer;
    }

    public void ReplaceAll(IEnumerable<Layer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);

        List<Layer> layerList = layers.ToList();

        if (layerList.Count == 0)
        {
            throw new InvalidOperationException(
                "A CAD document must contain at least one layer.");
        }

        if (layerList.All(layer => layer.Id != LayerId.Default))
        {
            throw new InvalidOperationException(
                "The default layer '0' cannot be removed.");
        }

        var duplicateId = layerList
            .GroupBy(layer => layer.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate layer id '{duplicateId.Key}'.");
        }

        var duplicateName = layerList
            .GroupBy(layer => layer.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate layer name '{duplicateName.Key}'.");
        }

        _layers.Clear();

        foreach (Layer layer in layerList)
        {
            _layers.Add(layer.Id, layer);
        }
    }

    public void SetVisibility(LayerId id, bool isVisible)
    {
        Layer layer = GetRequired(id);

        Replace(layer.WithVisibility(isVisible));
    }

    public void SetLocked(LayerId id, bool isLocked)
    {
        Layer layer = GetRequired(id);

        Replace(layer.WithLocked(isLocked));
    }
}
