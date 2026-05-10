using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using System.Collections.Generic;

namespace OpenCad2D.App.ViewModels.Layers;

public sealed class LayerManagerResult
{
    public LayerManagerResult(
        IReadOnlyList<Layer> layers,
        LayerId currentLayerId)
    {
        Layers = layers;
        CurrentLayerId = currentLayerId;
    }

    public IReadOnlyList<Layer> Layers { get; }

    public LayerId CurrentLayerId { get; }
}
