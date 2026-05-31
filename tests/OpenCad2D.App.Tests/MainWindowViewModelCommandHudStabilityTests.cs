using OpenCad2D.App.ViewModels;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Core.Entities;
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



    [Fact]
    public void CommandHudInput_SelectionOnlyModifyTools_ShouldNotExposeEditableNumericFields()
    {
        (string Command, Action<MainWindowViewModel>? PrepareTargetPhase)[] tools =
        [
            ("DELETE", null),
            ("EXPLODE", null),
            ("JOIN", null),
            ("TRIM", viewModel =>
            {
                viewModel.Workspace.ToolController.OnPointerPressed(
                    new PointerInfo(new Point2D(5, 0)));
            }),
            ("EXTEND", viewModel =>
            {
                viewModel.Workspace.ToolController.OnPointerPressed(
                    new PointerInfo(new Point2D(5, 0)));
            })
        ];

        foreach ((string command, Action<MainWindowViewModel>? prepareTargetPhase) in tools)
        {
            var viewModel = new MainWindowViewModel();
            viewModel.Workspace.Document.AddEntity(new LineEntity(
                new Point2D(0, 0),
                new Point2D(10, 0)));
            viewModel.SubmitCommandInput(command);

            prepareTargetPhase?.Invoke(viewModel);

            Assert.True(
                viewModel.CommandHudState.IsVisible,
                $"{command}: selection command should keep the HUD prompt visible.");
            Assert.Empty(GetEditableHudFieldKinds(viewModel));
            Assert.Empty(viewModel.CommandHudState.Fields);
        }
    }

    private static CommandHudFieldKind[] GetEditableHudFieldKinds(MainWindowViewModel viewModel)
    {
        return viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();
    }
}
