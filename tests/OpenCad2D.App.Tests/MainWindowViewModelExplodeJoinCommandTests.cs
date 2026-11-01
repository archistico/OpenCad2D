using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelExplodeJoinCommandTests
{
    [Fact]
    public void SubmitCommandInput_AfterJoinWithEnter_ShouldAcceptNextCommand()
    {
        var viewModel = new MainWindowViewModel();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(20, 0));
        viewModel.Workspace.Document.AddEntity(first);
        viewModel.Workspace.Document.AddEntity(second);

        viewModel.SubmitCommandInput("JOIN");
        viewModel.Workspace.ToolController.OnPointerPressed(new PointerInfo(new Point2D(5, 0)));
        viewModel.Workspace.ToolController.OnPointerPressed(new PointerInfo(new Point2D(15, 0)));
        viewModel.SubmitCommandInput(string.Empty);
        viewModel.SubmitCommandInput("L");

        Assert.IsType<LineTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void SubmitCommandInput_ExplodeWithPreselectionThenClickAnotherPolyline_ShouldExplodeBoth()
    {
        var viewModel = new MainWindowViewModel();
        var first = new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 5) });
        var second = new PolylineEntity(new[] { new Point2D(0, 20), new Point2D(10, 20), new Point2D(10, 25) });
        viewModel.Workspace.Document.AddEntity(first);
        viewModel.Workspace.Document.AddEntity(second);
        viewModel.Workspace.SelectionSet.Select(first.Id);

        viewModel.SubmitCommandInput("EXPLODE");
        viewModel.Workspace.ToolController.OnPointerPressed(new PointerInfo(new Point2D(5, 20)));
        viewModel.SubmitCommandInput(string.Empty);

        Assert.Empty(viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(4, viewModel.Workspace.Document.Entities.All.OfType<LineEntity>().Count());
    }
}
