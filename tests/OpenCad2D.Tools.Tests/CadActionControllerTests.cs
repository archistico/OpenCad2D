using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
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
    public void SelectAll_ShouldSelectOnlySelectableEntities()
    {
        CadDocument document = new();
        document.Layers.Add(Layer.Walls);
        document.Layers.Add(Layer.Axis.WithVisibility(false));
        document.Layers.Add(Layer.ConstructionLines.WithLocked(true));

        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var defaultLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var wallsLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1),
            layerId: LayerId.Walls);
        var hiddenLine = new LineEntity(
            new Point2D(0, 2),
            new Point2D(10, 2),
            layerId: LayerId.Axis);
        var lockedLine = new LineEntity(
            new Point2D(0, 3),
            new Point2D(10, 3),
            layerId: LayerId.ConstructionLines);

        document.AddEntities(new[]
        {
            defaultLine,
            wallsLine,
            hiddenLine,
            lockedLine
        });

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

        ToolResult result = actionController.SelectAll();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, selectionSet.Count);
        Assert.True(selectionSet.Contains(defaultLine.Id));
        Assert.True(selectionSet.Contains(wallsLine.Id));
        Assert.False(selectionSet.Contains(hiddenLine.Id));
        Assert.False(selectionSet.Contains(lockedLine.Id));
    }

    [Fact]
    public void SelectLast_ShouldRestorePreviousSingleEntitySelection()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var secondLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));

        document.AddEntities(new[]
        {
            firstLine,
            secondLine
        });

        selectionSet.ReplaceWith(firstLine.Id);
        selectionSet.Clear();

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

        ToolResult result = actionController.SelectLast();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Single(selectionSet.SelectedIds);
        Assert.True(selectionSet.Contains(firstLine.Id));
        Assert.False(selectionSet.Contains(secondLine.Id));
        Assert.Equal("Restored previous selection: 1 entity.", result.Message);
    }

    [Fact]
    public void SelectLast_ShouldRestorePreviousMultipleEntitySelection()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var secondLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));
        var thirdLine = new LineEntity(
            new Point2D(0, 2),
            new Point2D(10, 2));

        document.AddEntities(new[]
        {
            firstLine,
            secondLine,
            thirdLine
        });

        selectionSet.ReplaceWith(new[]
        {
            firstLine.Id,
            thirdLine.Id
        });
        selectionSet.Clear();

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

        ToolResult result = actionController.SelectLast();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, selectionSet.Count);
        Assert.True(selectionSet.Contains(firstLine.Id));
        Assert.True(selectionSet.Contains(thirdLine.Id));
        Assert.False(selectionSet.Contains(secondLine.Id));
        Assert.Equal("Restored previous selection: 2 entities.", result.Message);
    }

    [Fact]
    public void SelectLast_ShouldSkipPreviouslySelectedEntitiesThatAreNoLongerSelectable()
    {
        CadDocument document = new();
        document.Layers.Add(Layer.Axis.WithVisibility(false));

        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var selectableLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var hiddenLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1),
            layerId: LayerId.Axis);

        document.AddEntities(new[]
        {
            selectableLine,
            hiddenLine
        });

        selectionSet.ReplaceWith(new[]
        {
            selectableLine.Id,
            hiddenLine.Id
        });
        selectionSet.Clear();

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

        ToolResult result = actionController.SelectLast();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Single(selectionSet.SelectedIds);
        Assert.True(selectionSet.Contains(selectableLine.Id));
        Assert.False(selectionSet.Contains(hiddenLine.Id));
    }

    [Fact]
    public void SelectLast_WithNoPreviousSelection_ShouldLeaveCurrentSelectionUnchanged()
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

        ToolResult result = actionController.SelectLast();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(selectionSet.Contains(line.Id));
        Assert.Equal("No previous selectable selection found.", result.Message);
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



    [Fact]
    public void BringSelectionToFront_ShouldPutSelectedEntityOnTop()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var back = new LineEntity(new Point2D(0, 0), new Point2D(10, 0), drawOrder: 0);
        var selected = new LineEntity(new Point2D(0, 1), new Point2D(10, 1), drawOrder: 1);
        var front = new LineEntity(new Point2D(0, 2), new Point2D(10, 2), drawOrder: 2);

        document.AddEntities(new[] { back, selected, front });
        selectionSet.Select(selected.Id);

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.BringSelectionToFront();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.True(document.Entities.GetRequired(selected.Id).DrawOrder >
                    document.Entities.GetRequired(front.Id).DrawOrder);
        Assert.True(actionController.CanUndo);
    }

    [Fact]
    public void SendSelectionToBack_ShouldPutSelectedEntityAtBottom()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var back = new LineEntity(new Point2D(0, 0), new Point2D(10, 0), drawOrder: 0);
        var selected = new LineEntity(new Point2D(0, 1), new Point2D(10, 1), drawOrder: 1);
        var front = new LineEntity(new Point2D(0, 2), new Point2D(10, 2), drawOrder: 2);

        document.AddEntities(new[] { back, selected, front });
        selectionSet.Select(selected.Id);

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.SendSelectionToBack();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.True(document.Entities.GetRequired(selected.Id).DrawOrder <
                    document.Entities.GetRequired(back.Id).DrawOrder);
    }

    [Fact]
    public void BringSelectionForward_ShouldMoveSelectedEntityOneStepUp()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0), drawOrder: 0);
        var selected = new LineEntity(new Point2D(0, 1), new Point2D(10, 1), drawOrder: 1);
        var next = new LineEntity(new Point2D(0, 2), new Point2D(10, 2), drawOrder: 2);
        var top = new LineEntity(new Point2D(0, 3), new Point2D(10, 3), drawOrder: 3);

        document.AddEntities(new[] { first, selected, next, top });
        selectionSet.Select(selected.Id);

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.BringSelectionForward();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.True(document.Entities.GetRequired(selected.Id).DrawOrder >
                    document.Entities.GetRequired(next.Id).DrawOrder);
        Assert.True(document.Entities.GetRequired(selected.Id).DrawOrder <
                    document.Entities.GetRequired(top.Id).DrawOrder);
    }

    [Fact]
    public void SendSelectionBackward_ShouldMoveSelectedEntityOneStepDown()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var bottom = new LineEntity(new Point2D(0, 0), new Point2D(10, 0), drawOrder: 0);
        var previous = new LineEntity(new Point2D(0, 1), new Point2D(10, 1), drawOrder: 1);
        var selected = new LineEntity(new Point2D(0, 2), new Point2D(10, 2), drawOrder: 2);
        var top = new LineEntity(new Point2D(0, 3), new Point2D(10, 3), drawOrder: 3);

        document.AddEntities(new[] { bottom, previous, selected, top });
        selectionSet.Select(selected.Id);

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.SendSelectionBackward();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.True(document.Entities.GetRequired(selected.Id).DrawOrder <
                    document.Entities.GetRequired(previous.Id).DrawOrder);
        Assert.True(document.Entities.GetRequired(selected.Id).DrawOrder >
                    document.Entities.GetRequired(bottom.Id).DrawOrder);
    }

    [Fact]
    public void DrawOrderActions_ShouldBeUndoable()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var back = new LineEntity(new Point2D(0, 0), new Point2D(10, 0), drawOrder: 0);
        var selected = new LineEntity(new Point2D(0, 1), new Point2D(10, 1), drawOrder: 1);
        var front = new LineEntity(new Point2D(0, 2), new Point2D(10, 2), drawOrder: 2);

        document.AddEntities(new[] { back, selected, front });
        selectionSet.Select(selected.Id);

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        actionController.BringSelectionToFront();

        Assert.True(document.Entities.GetRequired(selected.Id).DrawOrder >
                    document.Entities.GetRequired(front.Id).DrawOrder);

        actionController.Undo();

        Assert.Equal(1, document.Entities.GetRequired(selected.Id).DrawOrder);
        Assert.Equal(2, document.Entities.GetRequired(front.Id).DrawOrder);
    }


    [Fact]
    public void AlignSelectionLeft_ShouldMoveEntitiesToSelectionMinX()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var left = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var right = new LineEntity(new Point2D(20, 10), new Point2D(30, 10));

        document.AddEntities(new[] { left, right });
        selectionSet.ReplaceWith(new[] { left.Id, right.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.AlignSelectionLeft();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, document.Entities.GetRequired(left.Id).GetBoundingBox().MinX);
        Assert.Equal(0, document.Entities.GetRequired(right.Id).GetBoundingBox().MinX);
        Assert.Equal(10, document.Entities.GetRequired(right.Id).GetBoundingBox().MinY);
        Assert.True(actionController.CanUndo);
    }

    [Fact]
    public void AlignSelectionRight_ShouldMoveEntitiesToSelectionMaxX()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var left = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var right = new LineEntity(new Point2D(20, 10), new Point2D(30, 10));

        document.AddEntities(new[] { left, right });
        selectionSet.ReplaceWith(new[] { left.Id, right.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.AlignSelectionRight();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(30, document.Entities.GetRequired(left.Id).GetBoundingBox().MaxX);
        Assert.Equal(30, document.Entities.GetRequired(right.Id).GetBoundingBox().MaxX);
    }

    [Fact]
    public void AlignSelectionTop_ShouldMoveEntitiesToVisualTopSelectionMinY()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var upper = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var lower = new LineEntity(new Point2D(20, 10), new Point2D(30, 10));

        document.AddEntities(new[] { upper, lower });
        selectionSet.ReplaceWith(new[] { upper.Id, lower.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.AlignSelectionTop();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, document.Entities.GetRequired(upper.Id).GetBoundingBox().MinY);
        Assert.Equal(0, document.Entities.GetRequired(lower.Id).GetBoundingBox().MinY);
    }

    [Fact]
    public void AlignSelectionBottom_ShouldMoveEntitiesToVisualBottomSelectionMaxY()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var upper = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var lower = new LineEntity(new Point2D(20, 10), new Point2D(30, 10));

        document.AddEntities(new[] { upper, lower });
        selectionSet.ReplaceWith(new[] { upper.Id, lower.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.AlignSelectionBottom();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(10, document.Entities.GetRequired(upper.Id).GetBoundingBox().MaxY);
        Assert.Equal(10, document.Entities.GetRequired(lower.Id).GetBoundingBox().MaxY);
    }

    [Fact]
    public void AlignSelectionLeft_WithSingleSelection_ShouldReturnNone()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.AlignSelectionLeft();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.False(actionController.CanUndo);
    }



    [Fact]
    public void DistributeSelectionHorizontally_ShouldEvenlySpaceCentersAndKeepEndsFixed()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var left = new LineEntity(new Point2D(-5, 0), new Point2D(5, 0));
        var middle = new LineEntity(new Point2D(65, 10), new Point2D(75, 10));
        var right = new LineEntity(new Point2D(95, 20), new Point2D(105, 20));

        document.AddEntities(new[] { left, middle, right });
        selectionSet.ReplaceWith(new[] { left.Id, middle.Id, right.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.DistributeSelectionHorizontally();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, document.Entities.GetRequired(left.Id).GetBoundingBox().Center.X);
        Assert.Equal(50, document.Entities.GetRequired(middle.Id).GetBoundingBox().Center.X);
        Assert.Equal(100, document.Entities.GetRequired(right.Id).GetBoundingBox().Center.X);
        Assert.Equal(10, document.Entities.GetRequired(middle.Id).GetBoundingBox().Center.Y);
        Assert.True(actionController.CanUndo);
    }

    [Fact]
    public void DistributeSelectionVertically_ShouldEvenlySpaceCentersAndKeepEndsFixed()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var top = new LineEntity(new Point2D(0, -5), new Point2D(10, -5));
        var middle = new LineEntity(new Point2D(20, 65), new Point2D(30, 65));
        var bottom = new LineEntity(new Point2D(40, 95), new Point2D(50, 95));

        document.AddEntities(new[] { top, middle, bottom });
        selectionSet.ReplaceWith(new[] { top.Id, middle.Id, bottom.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.DistributeSelectionVertically();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(-5, document.Entities.GetRequired(top.Id).GetBoundingBox().Center.Y);
        Assert.Equal(45, document.Entities.GetRequired(middle.Id).GetBoundingBox().Center.Y);
        Assert.Equal(95, document.Entities.GetRequired(bottom.Id).GetBoundingBox().Center.Y);
        Assert.Equal(25, document.Entities.GetRequired(middle.Id).GetBoundingBox().Center.X);
        Assert.True(actionController.CanUndo);
    }

    [Fact]
    public void DistributeSelectionHorizontally_WithTwoEntities_ShouldReturnNone()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(20, 0), new Point2D(30, 0));

        document.AddEntities(new[] { first, second });
        selectionSet.ReplaceWith(new[] { first.Id, second.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.DistributeSelectionHorizontally();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.False(actionController.CanUndo);
    }

    [Fact]
    public void DeselectAll_WithSelection_ShouldClearSelectionAndRememberLastSelection()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(0, 1), new Point2D(10, 1));

        document.AddEntities(new[] { first, second });
        selectionSet.ReplaceWith(new[] { first.Id, second.Id });

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.DeselectAll();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Deselected 2 entities.", result.Message);
        Assert.Empty(selectionSet.SelectedIds);
        Assert.Equal(new[] { first.Id, second.Id }, selectionSet.LastDeselectedSelectionIds);
    }

    [Fact]
    public void DeselectAll_WithEmptySelection_ShouldReturnNone()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        CadActionController actionController = CreateActionController(
            document,
            history,
            selectionSet);

        ToolResult result = actionController.DeselectAll();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("No selected entities to deselect.", result.Message);
    }

    private static CadActionController CreateActionController(
        CadDocument document,
        CommandHistory history,
        SelectionSet selectionSet)
    {
        ToolContext context = CreateContext(
            document,
            history,
            selectionSet);

        var toolController = new ToolController(
            context,
            new SelectionTool());

        return new CadActionController(
            context,
            toolController);
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