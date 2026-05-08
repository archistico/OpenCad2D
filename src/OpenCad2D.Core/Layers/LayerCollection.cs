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
}