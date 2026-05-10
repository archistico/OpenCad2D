using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class RotateToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForBasePoint()
    {
        var tool = new RotateTool();

        Assert.Equal("Rotate", tool.Name);
        Assert.Equal(RotateToolState.WaitingForBasePoint, tool.State);
        Assert.Null(tool.BasePoint);
        Assert.Null(tool.ReferencePoint);
        Assert.Null(tool.CurrentDestinationPoint);
    }

    [Fact]
    public void FirstPointerPress_WithoutSelection_ShouldNotStartTool()
    {
        var context = CreateContext();
        var tool = new RotateTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RotateToolState.WaitingForBasePoint, tool.State);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void FirstPointerPress_WithSelection_ShouldStoreBasePoint()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(RotateToolState.WaitingForReferencePoint, tool.State);
        Assert.Equal(new Point2D(5, 5), tool.BasePoint);
        Assert.Equal(new Point2D(5, 5), context.CurrentBasePoint);
    }

    [Fact]
    public void ReferencePointEqualToBasePoint_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RotateToolState.WaitingForReferencePoint, tool.State);
        Assert.Null(tool.ReferencePoint);
    }

    [Fact]
    public void SecondPointerPress_ShouldStoreReferencePoint()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(RotateToolState.WaitingForDestinationPoint, tool.State);
        Assert.Equal(new Point2D(10, 0), tool.ReferencePoint);
        Assert.Equal(new Point2D(10, 0), tool.CurrentDestinationPoint);
    }

    [Fact]
    public void PointerMove_AfterReferencePoint_ShouldUpdatePreview()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(90, tool.CurrentAngle.Degrees, precision: 6);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);
        var line = Assert.Single(preview.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    [Fact]
    public void ThirdPointerPress_ShouldRotateSelectedEntities()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(RotateToolState.WaitingForBasePoint, tool.State);
        Assert.Null(context.CurrentBasePoint);
        Assert.True(context.CommandHistory.CanUndo);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    [Fact]
    public void Rotate_ShouldBeUndoable()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        context.CommandHistory.Undo(context.Document);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    [Fact]
    public void PointerMove_WithOrthoEnabled_ShouldSnapAngleToNearestRightAngle()
    {
        var context = CreateContextWithSelectedLine();
        context.IsOrthoEnabled = true;
        var tool = new RotateTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(6, 8)));

        Assert.Equal(90, tool.CurrentAngle.Degrees, precision: 6);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);
        var line = Assert.Single(preview.OfType<LineEntity>());

        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
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
