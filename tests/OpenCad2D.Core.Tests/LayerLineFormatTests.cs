using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class LayerLineFormatTests
{
    [Fact]
    public void Constructor_ShouldStoreLineFormatId()
    {
        var layer = new Layer(
            new LayerId("Axis"),
            "Axis",
            LineFormatId.Axis);

        Assert.Equal(LineFormatId.Axis, layer.LineFormatId);
    }

    [Fact]
    public void Constructor_WithEmptyLineFormatId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new Layer(
                new LayerId("Invalid"),
                "Invalid",
                new LineFormatId("")));
    }

    [Fact]
    public void Default_ShouldUseContinuousLineFormat()
    {
        Assert.Equal(LineFormatId.Continuous, Layer.Default.LineFormatId);
        Assert.Equal(1.0, Layer.Default.LineWeight.Millimeters);
        Assert.Equal(CadColor.FromRgb(255, 255, 255), Layer.Default.Color);
    }

    [Fact]
    public void BuiltInLayers_ShouldUseExpectedLineFormats()
    {
        Assert.Equal(LineFormatId.Annotations, Layer.Annotations.LineFormatId);
        Assert.Equal(LineFormatId.Walls, Layer.Walls.LineFormatId);
        Assert.Equal(LineFormatId.Axis, Layer.Axis.LineFormatId);
        Assert.Equal(LineFormatId.Dashed, Layer.ConstructionLines.LineFormatId);
    }

    [Fact]
    public void WithLineFormat_ShouldKeepLayerDataAndChangeLineFormat()
    {
        var layer = new Layer(
            new LayerId("Walls"),
            "Walls",
            LineFormatId.Continuous,
            isVisible: false,
            isLocked: true);

        Layer changed = layer.WithLineFormat(LineFormatId.Dashed);

        Assert.Equal(layer.Id, changed.Id);
        Assert.Equal(layer.Name, changed.Name);
        Assert.Equal(LineFormatId.Dashed, changed.LineFormatId);
        Assert.Equal(layer.IsVisible, changed.IsVisible);
        Assert.Equal(layer.IsLocked, changed.IsLocked);
    }
}
