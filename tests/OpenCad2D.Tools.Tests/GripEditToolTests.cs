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

public sealed class GripEditToolPolylineVertexEditingTests
{
    [Fact]
    public void PointerPressed_OnInsertGrip_ShouldInsertPolylineVertex()
    {
        var context = CreateContext();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        context.Document.AddEntity(polyline);

        var tool = new GripEditTool(
            polyline.Id,
            new GripProviderRegistry());

        ToolResult selectInsertGrip = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult commitInsert = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(4, 2)));

        var edited = (PolylineEntity)context.Document.Entities.GetRequired(polyline.Id);

        Assert.Equal(ToolResultKind.Started, selectInsertGrip.Kind);
        Assert.Equal(ToolResultKind.Completed, commitInsert.Kind);
        Assert.Equal(4, edited.Vertices.Count);
        Assert.Equal(new Point2D(4, 2), edited.Vertices[1]);
        Assert.Equal(new Point2D(10, 0), edited.Vertices[2]);
    }

    [Fact]
    public void DeleteCurrentVertex_WhenHotPolylineVertexExists_ShouldDeleteVertex()
    {
        var context = CreateContext();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        context.Document.AddEntity(polyline);

        var tool = new GripEditTool(
            polyline.Id,
            new GripProviderRegistry());

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.DeleteCurrentVertex(context);

        var edited = (PolylineEntity)context.Document.Entities.GetRequired(polyline.Id);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, edited.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), edited.Vertices[0]);
        Assert.Equal(new Point2D(10, 10), edited.Vertices[1]);
    }

    [Fact]
    public void DeleteCurrentVertex_ShouldCreateUndoableCommand()
    {
        var document = new CadDocument();
        var history = new CommandHistory();
        var context = CreateContext(document, history);
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        document.AddEntity(polyline);

        var tool = new GripEditTool(
            polyline.Id,
            new GripProviderRegistry());

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.DeleteCurrentVertex(context);

        Assert.Equal(1, history.UndoCount);

        history.Undo(document);

        var restored = (PolylineEntity)document.Entities.GetRequired(polyline.Id);

        Assert.Equal(3, restored.Vertices.Count);
        Assert.Equal(new Point2D(10, 0), restored.Vertices[1]);
    }

    [Fact]
    public void DeleteCurrentVertex_WhenPolylineHasMinimumVertices_ShouldReturnNone()
    {
        var context = CreateContext();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        context.Document.AddEntity(polyline);

        var tool = new GripEditTool(
            polyline.Id,
            new GripProviderRegistry());

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.DeleteCurrentVertex(context);

        var edited = (PolylineEntity)context.Document.Entities.GetRequired(polyline.Id);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(2, edited.Vertices.Count);
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
