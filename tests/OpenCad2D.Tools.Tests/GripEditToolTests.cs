using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Grips;

namespace OpenCad2D.Tools.Tests;

public sealed class GripEditToolTests
{
    [Fact]
    public void PointerMoved_WhenCursorIsNearGrip_ShouldSetHotGrip()
    {
        var context = CreateContext();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(line);

        var tool = new GripEditTool(
            line.Id,
            new GripProviderRegistry());

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0.5, 0.5)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(0, tool.HotGripIndex);
        Assert.Null(tool.WarmGripIndex);
    }

    [Fact]
    public void PointerPressed_WhenIdleAndCursorIsNearGrip_ShouldActivateGripAndSetBasePoint()
    {
        var context = CreateContext();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(line);

        var tool = new GripEditTool(
            line.Id,
            new GripProviderRegistry());

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0.5, 0.5)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(0, tool.WarmGripIndex);
        Assert.Equal(new Point2D(0, 0), context.CurrentBasePoint);
    }

    [Fact]
    public void PointerPressed_WhenGripIsActive_ShouldReplaceEntityAndPreserveId()
    {
        var context = CreateContext();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(line);

        var tool = new GripEditTool(
            line.Id,
            new GripProviderRegistry());

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var edited = (LineEntity)context.Document.Entities.GetRequired(line.Id);

        Assert.Equal(line.Id, edited.Id);
        Assert.Equal(new Point2D(5, 5), edited.Start);
        Assert.Equal(new Point2D(10, 0), edited.End);
        Assert.Null(context.CurrentBasePoint);
        Assert.Null(tool.WarmGripIndex);
    }

    [Fact]
    public void PointerPressed_WhenGripIsActive_ShouldCreateUndoableCommand()
    {
        var document = new CadDocument();
        var history = new CommandHistory();
        var context = CreateContext(document, history);
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var tool = new GripEditTool(
            line.Id,
            new GripProviderRegistry());

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(1, history.UndoCount);

        history.Undo(document);

        var restored = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(10, 0), restored.End);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        CommandHistory? history = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            history ?? new CommandHistory(),
            new SnapService(),
            selectionTolerance: 2,
            snapTolerance: 0);
    }
}
