using OpenCad2D.App.ViewModels;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelOffsetCommandTests
{
    [Fact]
    public void SubmitCommandInput_OffsetTypedFirstPointThenTypedDistance_ShouldSetDistance()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("OFFSET");
        viewModel.SubmitCommandInput("10,20");

        var tool = Assert.IsType<OffsetTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(OffsetToolState.WaitingForDistanceSecondPoint, tool.State);

        viewModel.SubmitCommandInput("25");

        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(25, tool.Distance);
    }

    [Fact]
    public void SubmitCommandInput_OffsetTypedFirstPointThenTypedSecondPoint_ShouldMeasureDistance()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("OFFSET");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("3,4");

        var tool = Assert.IsType<OffsetTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(5, tool.Distance!.Value, 9);
    }

    [Fact]
    public void CommandHudInput_OffsetDistanceDuringSecondPoint_ShouldBeAccepted()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("OFFSET");
        viewModel.SubmitCommandInput("0,0");

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "7",
            confirm: true,
            out _);

        var tool = Assert.IsType<OffsetTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.True(handled);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(7, tool.Distance);
    }

    [Fact]
    public void SubmitCommandInput_OffsetEmptyEnterAfterDistance_ShouldReuseLastDistance()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("OFFSET");
        viewModel.SubmitCommandInput("4");
        viewModel.SubmitCommandInput(string.Empty);

        Assert.IsNotType<OffsetTool>(viewModel.Workspace.ToolController.ActiveTool);

        viewModel.SubmitCommandInput("OFFSET");
        viewModel.SubmitCommandInput(string.Empty);

        var tool = Assert.IsType<OffsetTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(4, tool.Distance);
    }
}
