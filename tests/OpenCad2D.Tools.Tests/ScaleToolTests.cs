using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class ScaleToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForBasePoint()
    {
        var tool = new ScaleTool();

        Assert.Equal("Scale", tool.Name);
        Assert.Equal(ScaleToolState.WaitingForBasePoint, tool.State);
        Assert.Null(tool.BasePoint);
        Assert.Null(tool.ReferencePoint);
        Assert.Null(tool.CurrentDestinationPoint);
        Assert.Equal(1.0, tool.CurrentFactor);
    }

    [Fact]
    public void FirstPointerPress_WithoutSelection_ShouldEnterEntitySelection()
    {
        var context = CreateContext();
        var tool = new ScaleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForEntitySelection, tool.State);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void FirstPointerPress_WithoutInitialSelection_ShouldSelectEntityFirst()
    {
        var context = CreateContextWithLine();
        var tool = new ScaleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForEntitySelection, tool.State);
        Assert.True(context.Selection.HasSelection);
    }

    [Fact]
    public void ConfirmEntitySelection_AfterSelectingEntity_ShouldAskForBasePoint()
    {
        var context = CreateContextWithLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.ConfirmEntitySelection(context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForBasePoint, tool.State);
        Assert.Contains("base point", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetActiveSnapKind_WhenSelectingEntities_ShouldUseEntityOnlySnap()
    {
        var context = CreateContext();
        context.EnabledSnaps = SnapKind.Endpoint | SnapKind.Entity;
        var tool = new ScaleTool();

        SnapKind snapKind = tool.GetActiveSnapKind(context);

        Assert.Equal(SnapKind.EntityOnly, snapKind);
    }

    [Fact]
    public void FirstPointerPress_WithSelection_ShouldStoreBasePoint()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForReferencePoint, tool.State);
        Assert.Equal(new Point2D(0, 0), tool.BasePoint);
        Assert.Equal(new Point2D(0, 0), context.CurrentBasePoint);
    }

    [Fact]
    public void ReferencePointEqualToBasePoint_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForReferencePoint, tool.State);
        Assert.Null(tool.ReferencePoint);
    }

    [Fact]
    public void SecondPointerPress_ShouldStoreReferencePoint()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForDestinationPoint, tool.State);
        Assert.Equal(new Point2D(10, 0), tool.ReferencePoint);
        Assert.Equal(new Point2D(10, 0), tool.CurrentDestinationPoint);
        Assert.Equal(1.0, tool.CurrentFactor);
    }

    [Fact]
    public void PointerMove_AfterReferencePoint_ShouldUpdatePreview()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(20, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(2.0, tool.CurrentFactor, precision: 6);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);
        var line = Assert.Single(preview.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(20, 0), line.End);
    }

    [Fact]
    public void DestinationPointEqualToBasePoint_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForDestinationPoint, tool.State);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ThirdPointerPress_ShouldScaleSelectedEntities()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(20, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForBasePoint, tool.State);
        Assert.Null(context.CurrentBasePoint);
        Assert.True(context.CommandHistory.CanUndo);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(20, 0), line.End);
    }

    [Fact]
    public void Scale_ShouldBeUndoable()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(20, 0)));

        context.CommandHistory.Undo(context.Document);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }

    private static ToolContext CreateContextWithLine()
    {
        var document = new CadDocument();
        var commandHistory = new CommandHistory();
        var context = new ToolContext(
            document,
            commandHistory,
            new SnapService());

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        return context;
    }

    private static ToolContext CreateContextWithSelectedLine()
    {
        var document = new CadDocument();
        var commandHistory = new CommandHistory();
        var context = new ToolContext(
            document,
            commandHistory,
            new SnapService());

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        context.Selection.Set.Select(line.Id);

        return context;
    }
}
