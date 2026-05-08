using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;

namespace OpenCad2D.Core.Tests;

public sealed class LayerCollectionTests
{
    [Fact]
    public void Constructor_ShouldCreateDefaultLayer()
    {
        var layers = new LayerCollection();

        Assert.Equal(1, layers.Count);
        Assert.True(layers.Contains(LayerId.Default));
        Assert.Equal("0", layers.Default.Name);
    }

    [Fact]
    public void Add_ShouldAddLayer()
    {
        var layers = new LayerCollection();

        var layer = new Layer(
            new LayerId("Walls"),
            "Walls");

        layers.Add(layer);

        Assert.Equal(2, layers.Count);
        Assert.True(layers.Contains(new LayerId("Walls")));
    }

    [Fact]
    public void Add_WithDuplicateId_ShouldThrow()
    {
        var layers = new LayerCollection();

        var layer = new Layer(
            new LayerId("Walls"),
            "Walls");

        layers.Add(layer);

        Assert.Throws<InvalidOperationException>(() =>
            layers.Add(layer));
    }

    [Fact]
    public void GetRequired_WithExistingLayer_ShouldReturnLayer()
    {
        var layers = new LayerCollection();

        var layer = new Layer(
            new LayerId("Walls"),
            "Walls");

        layers.Add(layer);

        Layer result = layers.GetRequired(new LayerId("Walls"));

        Assert.Equal("Walls", result.Name);
    }

    [Fact]
    public void GetRequired_WithMissingLayer_ShouldThrow()
    {
        var layers = new LayerCollection();

        Assert.Throws<KeyNotFoundException>(() =>
            layers.GetRequired(new LayerId("Missing")));
    }
}