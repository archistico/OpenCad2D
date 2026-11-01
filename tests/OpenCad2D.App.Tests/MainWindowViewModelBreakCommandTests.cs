using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelBreakCommandTests
{
    [Fact]
    public void SubmitCommandInput_BreakPointWithPreselectedLine_ShouldBreakAtTypedPoint()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("BREAKPOINT");
        viewModel.SubmitCommandInput("4,0");

        Assert.Equal("Entity broken at point.", viewModel.LastMessage);
        Assert.Equal(2, viewModel.Workspace.Document.Entities.All.OfType<LineEntity>().Count());
    }

    [Fact]
    public void SubmitCommandInput_EnterAtBreakTargetPrompt_ShouldFinishSoNextCommandCanStart()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("BREAK");
        viewModel.SubmitCommandInput(string.Empty);
        viewModel.SubmitCommandInput("L");

        Assert.IsType<LineTool>(viewModel.Workspace.ToolController.ActiveTool);
    }
}
