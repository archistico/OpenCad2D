using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class CadActionControllerTests
{
    [Fact]
    public void Constructor_ShouldExposeInitialState()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        var actionController = new CadActionController(
            context,
            toolController);

        Assert.False(actionController.CanUndo);
        Assert.False(actionController.CanRedo);
        Assert.False(actionController.HasSelection);
    }

    [Fact]
    public void Undo_WithNoUndoAvailable_ShouldReturnNone()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        var actionController = new CadActionController(
            context,
            toolController);

        ToolResult result = actionController.Undo();

        Assert.Equal(ToolResultKind.None, result.Kind);
    }

    [Fact]
    public void Redo_WithNoRedoAvailable_ShouldReturnNone()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        var actionController = new CadActionController(
            context,
            toolController);

        ToolResult result = actionController.Redo();

        Assert.Equal(ToolResultKind.None, result.Kind);
    }

    [Fact]
    public void Undo_AfterLineCreation_ShouldRemoveLine()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new LineTool());

        var actionController = new CadActionController(
            context,
            toolController);

        toolController.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        toolController.OnPointerPressed(
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(1, document.Entities.Count);
        Assert.True(actionController.CanUndo);

        ToolResult result = actionController.Undo();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, document.Entities.Count);
        Assert.True(actionController.CanRedo);
    }

    [Fact]
    public void Redo_AfterUndo_ShouldRestoreLine()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new LineTool());

        var actionController = new CadActionController(
            context,
            toolController);

        toolController.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        toolController.OnPointerPressed(
            new PointerInfo(new Point2D(10, 0)));

        actionController.Undo();

        Assert.Equal(0, document.Entities.Count);
        Assert.True(actionController.CanRedo);

        ToolResult result = actionController.Redo();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, document.Entities.Count);
        Assert.True(actionController.CanUndo);
    }

    [Fact]
    public void DeleteSelection_WithNoSelection_ShouldReturnNone()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        var actionController = new CadActionController(
            context,
            toolController);

        ToolResult result = actionController.DeleteSelection();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, document.Entities.Count);
    }

    [Fact]
    public void DeleteSelection_WithSelectedEntity_ShouldDeleteAndClearSelection()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        var actionController = new CadActionController(
            context,
            toolController);

        Assert.True(actionController.HasSelection);

        ToolResult result = actionController.DeleteSelection();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, document.Entities.Count);
        Assert.True(selectionSet.IsEmpty);
        Assert.False(actionController.HasSelection);
        Assert.True(actionController.CanUndo);
    }

    [Fact]
    public void DeleteSelection_ShouldBeUndoable()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        var actionController = new CadActionController(
            context,
            toolController);

        actionController.DeleteSelection();

        Assert.Equal(0, document.Entities.Count);

        actionController.Undo();

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void CancelActiveTool_ShouldCancelToolThroughToolController()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new LineTool());

        var actionController = new CadActionController(
            context,
            toolController);

        toolController.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        var lineTool = Assert.IsType<LineTool>(toolController.ActiveTool);

        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, lineTool.State);

        ToolResult result = actionController.CancelActiveTool();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, lineTool.State);
    }

    [Fact]
    public void CancelActiveTool_WhenSelectionToolIsActive_ShouldClearSelection()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        var actionController = new CadActionController(
            context,
            toolController);

        Assert.True(actionController.HasSelection);

        ToolResult result = actionController.CancelActiveTool();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.True(selectionSet.IsEmpty);
        Assert.False(actionController.HasSelection);
    }

    private static ToolContext CreateContext(
        CadDocument document,
        CommandHistory history,
        SelectionSet selectionSet)
    {
        return new ToolContext(
            document,
            history,
            new SnapService(),
            selectionSet: selectionSet,
            selectionTolerance: 1);
    }
}