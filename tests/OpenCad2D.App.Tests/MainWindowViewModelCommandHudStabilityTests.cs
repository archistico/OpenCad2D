using OpenCad2D.App.ViewModels;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using System.Linq;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelCommandHudStabilityTests
{
    [Fact]
    public void CommandHudInput_PolygonSides_ShouldExposeAndAcceptSidesField()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetTool(ToolId.Polygon);

        CommandHudFieldKind[] initialKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Equal(new[] { CommandHudFieldKind.Sides }, initialKinds);

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Sides,
            "5",
            confirm: true,
            out _);

        Assert.True(handled);
        PolygonTool polygonTool = Assert.IsType<PolygonTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(PolygonToolState.WaitingForCenter, polygonTool.State);
        Assert.Equal(5, polygonTool.SideCount);
    }

    [Fact]
    public void CommandHudInput_PolygonSides_ShouldRejectNonWholeValues()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetTool(ToolId.Polygon);

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Sides,
            "4.5",
            confirm: true,
            out _);

        Assert.True(handled);
        PolygonTool polygonTool = Assert.IsType<PolygonTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(PolygonToolState.WaitingForSides, polygonTool.State);
        Assert.Contains("whole number", viewModel.LastMessage);
    }

    private static CommandHudFieldKind[] GetEditableHudFieldKinds(MainWindowViewModel viewModel)
    {
        return viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();
    }
}
