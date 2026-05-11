using System.Linq;
using OpenCad2D.App.ViewModels.Layers;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;

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
}
