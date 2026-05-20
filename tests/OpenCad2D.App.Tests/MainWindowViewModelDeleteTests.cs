using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelDeleteTests
{
    [Fact]
    public void DeleteSelection_WithExistingSelection_ShouldDeleteImmediately()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.DeleteSelection();

        Assert.False(viewModel.Workspace.Document.Entities.Contains(line.Id));
        Assert.True(viewModel.Workspace.SelectionSet.IsEmpty);
    }

    [Fact]
    public void DeleteSelection_WithNoSelection_ShouldStartDeleteTool()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.DeleteSelection();

        Assert.Equal("Delete", viewModel.ActiveToolName);
        Assert.Contains("Select entities to delete", viewModel.LastMessage);
    }
}
