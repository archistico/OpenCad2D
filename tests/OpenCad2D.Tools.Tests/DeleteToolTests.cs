using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class DeleteToolTests
{
    [Fact]
    public void Constructor_ShouldHaveName()
    {
        var tool = new DeleteTool();

        Assert.Equal("Delete", tool.Name);
    }

    [Fact]
    public void Execute_WithNoSelection_ShouldReturnNone()
    {
        var context = CreateContext();
        var tool = new DeleteTool();

        ToolResult result = tool.Execute(context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void Execute_WithSelection_ShouldDeleteSelectedEntity()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var selectedLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var unselectedLine = new LineEntity(
            new Point2D(20, 0),
            new Point2D(30, 0));

        document.AddEntity(selectedLine);
        document.AddEntity(unselectedLine);

        selectionSet.Select(selectedLine.Id);

        var context = CreateContext(document, selectionSet);
        var tool = new DeleteTool();

        ToolResult result = tool.Execute(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, document.Entities.Count);
        Assert.False(document.Entities.Contains(selectedLine.Id));
        Assert.True(document.Entities.Contains(unselectedLine.Id));
        Assert.True(selectionSet.IsEmpty);
    }

    [Fact]
    public void Execute_WithMultipleSelection_ShouldDeleteSelectedEntities()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new CircleEntity(
            new Point2D(5, 5),
            3);

        var third = new LineEntity(
            new Point2D(20, 0),
            new Point2D(30, 0));

        document.AddEntity(first);
        document.AddEntity(second);
        document.AddEntity(third);

        selectionSet.Select(first.Id);
        selectionSet.Select(second.Id);

        var context = CreateContext(document, selectionSet);
        var tool = new DeleteTool();

        ToolResult result = tool.Execute(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, document.Entities.Count);
        Assert.False(document.Entities.Contains(first.Id));
        Assert.False(document.Entities.Contains(second.Id));
        Assert.True(document.Entities.Contains(third.Id));
        Assert.True(selectionSet.IsEmpty);
    }

    [Fact]
    public void Execute_ShouldBeUndoable()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();
        CommandHistory history = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        var context = CreateContext(
            document,
            selectionSet,
            history);

        var tool = new DeleteTool();

        tool.Execute(context);

        Assert.Equal(0, document.Entities.Count);
        Assert.True(history.CanUndo);

        history.Undo(document);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void Execute_AfterUndo_ShouldBeRedoable()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();
        CommandHistory history = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        var context = CreateContext(
            document,
            selectionSet,
            history);

        var tool = new DeleteTool();

        tool.Execute(context);

        history.Undo(document);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(history.CanRedo);

        history.Redo(document);

        Assert.Equal(0, document.Entities.Count);
    }

    [Fact]
    public void OnPointerPressed_ShouldExecuteDelete()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        var context = CreateContext(document, selectionSet);
        var tool = new DeleteTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, document.Entities.Count);
        Assert.True(selectionSet.IsEmpty);
    }

    [Fact]
    public void OnPointerMoved_ShouldReturnNone()
    {
        var context = CreateContext();
        var tool = new DeleteTool();

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
    }

    [Fact]
    public void Cancel_ShouldNotDeleteSelection()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        var context = CreateContext(document, selectionSet);
        var tool = new DeleteTool();

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
        Assert.True(selectionSet.Contains(line.Id));
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SelectionSet? selectionSet = null,
        CommandHistory? history = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            history ?? new CommandHistory(),
            new SnapService(),
            selectionSet: selectionSet);
    }
}