using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelDefaultDrawingTests
{
    [Fact]
    public void Constructor_ShouldCreateDefaultCadLayers()
    {
        var viewModel = new MainWindowViewModel();

        AssertDefaultLayer(
            viewModel,
            LayerId.Default,
            "0",
            LineFormatId.Continuous);
        AssertDefaultLayer(
            viewModel,
            LayerId.Annotations,
            "Annotations",
            LineFormatId.Annotations);
        AssertDefaultLayer(
            viewModel,
            LayerId.Walls,
            "Walls",
            LineFormatId.Walls);
        AssertDefaultLayer(
            viewModel,
            LayerId.Axis,
            "Axis",
            LineFormatId.Axis);
        AssertDefaultLayer(
            viewModel,
            LayerId.ConstructionLines,
            "Construction lines",
            LineFormatId.Dashed);
    }

    [Fact]
    public void Constructor_ShouldStartWithEmptyDrawing()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.Equal("Untitled", viewModel.CurrentFileName);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void NewDocument_ShouldKeepDefaultCadLayers()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.NewDocument();

        Assert.True(viewModel.Workspace.Document.Layers.Contains(LayerId.Annotations));
        Assert.True(viewModel.Workspace.Document.Layers.Contains(LayerId.Walls));
        Assert.True(viewModel.Workspace.Document.Layers.Contains(LayerId.Axis));
        Assert.True(viewModel.Workspace.Document.Layers.Contains(LayerId.ConstructionLines));
        Assert.Empty(viewModel.Workspace.Document.Entities.All);
    }

    private static void AssertDefaultLayer(
        MainWindowViewModel viewModel,
        LayerId layerId,
        string expectedName,
        LineFormatId expectedLineFormatId)
    {
        Layer layer = viewModel.Workspace.Document.Layers.GetRequired(layerId);

        Assert.Equal(expectedName, layer.Name);
        Assert.Equal(expectedLineFormatId, layer.LineFormatId);
    }
}
