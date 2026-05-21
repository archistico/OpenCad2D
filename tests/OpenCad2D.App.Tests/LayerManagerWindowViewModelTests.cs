using System.Linq;
using OpenCad2D.App.ViewModels.Layers;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.App.Tests;

public sealed class LayerManagerWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldSelectLayerLineFormat()
    {
        var document = new CadDocument();
        var layerId = new LayerId("axes");

        document.Layers.Add(new Layer(
            layerId,
            "Axes",
            LineFormatId.Axis));

        var viewModel = new LayerManagerWindowViewModel(
            document,
            layerId);

        EditableLayerViewModel layer = viewModel.Layers.Single(item => item.Id == layerId);

        Assert.Equal(
            LineFormatId.Axis,
            layer.SelectedLineFormat.Id);
    }

    [Fact]
    public void Constructor_ShouldExposeAllDocumentLineFormatsToEachLayer()
    {
        var document = new CadDocument();
        var layerId = new LayerId("dashed");

        document.Layers.Add(new Layer(
            layerId,
            "Dashed",
            LineFormatId.Dashed));

        var viewModel = new LayerManagerWindowViewModel(
            document,
            layerId);

        EditableLayerViewModel layer = viewModel.Layers.Single(item => item.Id == layerId);

        Assert.Equal(
            document.LineFormats.Count,
            layer.AvailableLineFormats.Count);
        Assert.Contains(
            layer.AvailableLineFormats,
            item => item.Id == LineFormatId.Dashed);
    }

    [Fact]
    public void TryBuildResult_ShouldUseSelectedLineFormat()
    {
        var document = new CadDocument();
        var layerId = new LayerId("walls");

        document.Layers.Add(new Layer(
            layerId,
            "Walls",
            LineFormatId.Continuous));

        var viewModel = new LayerManagerWindowViewModel(
            document,
            layerId);

        EditableLayerViewModel layer = viewModel.Layers.Single(item => item.Id == layerId);
        layer.SelectedLineFormat = layer.AvailableLineFormats.Single(format => format.Id == LineFormatId.DashDot);

        bool success = viewModel.TryBuildResult(out LayerManagerResult result);

        Assert.True(success);
        Assert.Equal(
            LineFormatId.DashDot,
            result.Layers.Single(item => item.Id == layerId).LineFormatId);
    }

    [Fact]
    public void AddLayer_ShouldUseContinuousLineFormat()
    {
        var document = new CadDocument();
        var viewModel = new LayerManagerWindowViewModel(
            document,
            LayerId.Default);

        viewModel.AddLayer();

        EditableLayerViewModel addedLayer = viewModel.SelectedLayer!;

        Assert.Equal(
            LineFormatId.Continuous,
            addedLayer.SelectedLineFormat.Id);
    }

    [Fact]
    public void Constructor_ShouldExposeLayerFillColor()
    {
        var document = new CadDocument();
        var layerId = new LayerId("filled");
        CadColor fillColor = CadColor.FromRgb(0x11, 0x22, 0x33);

        document.Layers.Add(new Layer(
            layerId,
            "Filled",
            LineFormatId.Continuous,
            fillColor: fillColor));

        var viewModel = new LayerManagerWindowViewModel(
            document,
            layerId);

        EditableLayerViewModel layer = viewModel.Layers.Single(item => item.Id == layerId);

        Assert.Equal("#112233", layer.FillColorHex);
    }

    [Fact]
    public void TryBuildResult_ShouldUseEditedFillColor()
    {
        var document = new CadDocument();
        var layerId = new LayerId("filled");

        document.Layers.Add(new Layer(
            layerId,
            "Filled",
            LineFormatId.Continuous));

        var viewModel = new LayerManagerWindowViewModel(
            document,
            layerId);

        EditableLayerViewModel layer = viewModel.Layers.Single(item => item.Id == layerId);
        layer.FillColorHex = "#445566";

        bool success = viewModel.TryBuildResult(out LayerManagerResult result);

        Assert.True(success);
        Assert.Equal(
            CadColor.FromRgb(0x44, 0x55, 0x66),
            result.Layers.Single(item => item.Id == layerId).FillColor);
    }

    [Fact]
    public void TryBuildResult_ShouldRejectInvalidFillColor()
    {
        var document = new CadDocument();
        var layerId = new LayerId("filled");

        document.Layers.Add(new Layer(
            layerId,
            "Filled",
            LineFormatId.Continuous));

        var viewModel = new LayerManagerWindowViewModel(
            document,
            layerId);

        EditableLayerViewModel layer = viewModel.Layers.Single(item => item.Id == layerId);
        layer.FillColorHex = "yellow";

        bool success = viewModel.TryBuildResult(out _);

        Assert.False(success);
        Assert.Equal(
            "Layer 'Filled' has an invalid fill color. Use #RRGGBB.",
            viewModel.ValidationMessage);
    }

}
