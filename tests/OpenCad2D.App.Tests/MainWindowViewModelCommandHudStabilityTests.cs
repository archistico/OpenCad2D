using OpenCad2D.App.ViewModels;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Core.Entities;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using System.Linq;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelCommandHudStabilityTests
{
    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e999")]
    [InlineData("-")]
    [InlineData("")]
    public async Task CommandHudInput_InvalidNumber_ShouldReportHandledButNotAccepted(string input)
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SubmitCommandInput("LINE");
        viewModel.SubmitCommandInput("0,0");

        var commit = await viewModel.TryCommitCommandHudFieldInputAsync(CommandHudFieldKind.Distance, input, true);

        Assert.True(commit.Handled);
        Assert.False(commit.Accepted);
        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.False(viewModel.HasCommandHudInputOverrides);
    }

    [Theory]
    [InlineData("257")]
    [InlineData("2147483648")]
    [InlineData("4.0000001")]
    public void CommandHudInput_InvalidSideCount_ShouldNotStoreOrAdvance(string input)
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SubmitCommandInput("POLYGON");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(CommandHudFieldKind.Sides, input, false, out _, out bool accepted));

        Assert.False(accepted);
        Assert.False(viewModel.HasCommandHudInputOverrides);
        Assert.Equal(PolygonToolState.WaitingForSides, Assert.IsType<PolygonTool>(viewModel.Workspace.ToolController.ActiveTool).State);
    }

    [Fact]
    public void CommandHudInput_FirstRectangleDimensionWithoutPointer_ShouldAcceptBeforeHeight()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SubmitCommandInput("RECTANGLE");
        viewModel.SubmitCommandInput("0,0");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(CommandHudFieldKind.Width, "20", false, out _, out bool accepted));
        Assert.True(accepted);
        Assert.True(viewModel.TryCommitCommandHudFieldInput(CommandHudFieldKind.Height, "10", true, out _, out accepted));
        Assert.True(accepted);
        PolylineEntity rectangle = Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(new Point2D(20, 10), rectangle.Vertices[2]);
    }

    [Fact]
    public void CommandHudInput_PointerUpdates_ShouldReuseFieldsAndRows()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SubmitCommandInput("LINE");
        viewModel.SubmitCommandInput("0,0");
        CommandHudStateViewModel initial = viewModel.CommandHudState;

        viewModel.SetMousePosition(new Point2D(12, 5));
        CommandHudStateViewModel updated = viewModel.CommandHudState;

        Assert.Same(initial.FieldRows, updated.FieldRows);
        Assert.Same(initial.Fields[0], updated.Fields[0]);
        Assert.Equal(13, updated.Fields[0].LiveValue);
    }

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
