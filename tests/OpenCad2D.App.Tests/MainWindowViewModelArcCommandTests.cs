using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelArcCommandTests
{
    [Fact]
    public void SubmitCommandInput_ArcTypedEndAngle_ShouldCreateArc()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("ARC");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("10,0");
        viewModel.SubmitCommandInput("90");

        ArcEntity arc = Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(0, arc.Geometry.EndPoint.X, precision: 9);
        Assert.Equal(10, arc.Geometry.EndPoint.Y, precision: 9);
    }

    [Fact]
    public void SubmitCommandInput_ArcDirectionOption_ShouldCreateClockwiseArcFromHudAngle()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("ARC");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("10,0");
        viewModel.SubmitCommandInput("D");

        Assert.Contains(viewModel.CommandHudState.Options, option => option.Keyword == "Direction");
        Assert.Contains("(clockwise)", viewModel.CommandHudState.Prompt);

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "90",
            confirm: true,
            out _));

        ArcEntity arc = Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<ArcEntity>());
        Assert.False(arc.IsCounterClockwise);
        Assert.Equal(0, arc.Geometry.EndPoint.X, precision: 9);
        Assert.Equal(10, arc.Geometry.EndPoint.Y, precision: 9);
    }

    [Fact]
    public void SubmitCommandInput_EnterAtArcCenterPrompt_ShouldFinishSoNextCommandCanStart()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("ARC");
        viewModel.SubmitCommandInput(string.Empty);
        viewModel.SubmitCommandInput("L");

        Assert.IsType<LineTool>(viewModel.Workspace.ToolController.ActiveTool);
    }
}
