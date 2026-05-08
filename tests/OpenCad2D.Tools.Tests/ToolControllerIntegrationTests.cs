using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class ToolControllerIntegrationTests
{
    [Fact]
    public void SelectionToolThenMoveTool_ShouldSelectAndMoveEntity()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var context = new ToolContext(
            document,
            history,
            new SnapService(),
            selectionSet: selectionSet,
            selectionTolerance: 1);

        var controller = new ToolController(
            context,
            new SelectionTool());

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0.2)));

        ToolResult selectionResult = controller.OnPointerReleased(
            new PointerInfo(new Point2D(5, 0.2)));

        Assert.Equal(ToolResultKind.Updated, selectionResult.Kind);
        Assert.True(selectionSet.Contains(line.Id));
        Assert.Equal(1, selectionSet.Count);

        controller.SetActiveTool(
            new MoveTool(),
            cancelCurrentTool: false);

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult moveResult = controller.OnPointerPressed(
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, moveResult.Kind);

        var moved = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 2), moved.Start);
        Assert.Equal(new Point2D(15, 2), moved.End);

        Assert.True(history.CanUndo);

        history.Undo(document);

        var restored = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(10, 0), restored.End);
    }

    [Fact]
    public void SelectionToolThenCopyTool_ShouldSelectAndCopyEntity()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var context = new ToolContext(
            document,
            history,
            new SnapService(),
            selectionSet: selectionSet,
            selectionTolerance: 1);

        var controller = new ToolController(
            context,
            new SelectionTool());

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0.2)));

        ToolResult selectionResult = controller.OnPointerReleased(
            new PointerInfo(new Point2D(5, 0.2)));

        Assert.Equal(ToolResultKind.Updated, selectionResult.Kind);
        Assert.True(selectionSet.Contains(line.Id));
        Assert.Equal(1, selectionSet.Count);

        controller.SetActiveTool(
            new CopyTool(),
            cancelCurrentTool: false);

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult copyResult = controller.OnPointerPressed(
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, copyResult.Kind);
        Assert.Equal(2, document.Entities.Count);

        var lines = document.Entities.All
            .OfType<LineEntity>()
            .ToList();

        var original = lines.Single(entity => entity.Id == line.Id);
        var copied = lines.Single(entity => entity.Id != line.Id);

        Assert.Equal(new Point2D(0, 0), original.Start);
        Assert.Equal(new Point2D(10, 0), original.End);

        Assert.Equal(new Point2D(5, 2), copied.Start);
        Assert.Equal(new Point2D(15, 2), copied.End);

        Assert.True(history.CanUndo);

        history.Undo(document);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void LineToolThenSelectionToolThenMoveTool_ShouldDrawSelectAndMoveLine()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var context = new ToolContext(
            document,
            history,
            new SnapService(),
            selectionSet: selectionSet,
            selectionTolerance: 1);

        var controller = new ToolController(
            context,
            new LineTool());

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult lineResult = controller.OnPointerPressed(
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, lineResult.Kind);
        Assert.Equal(1, document.Entities.Count);

        var createdLine = Assert.Single(
            document.Entities.All.OfType<LineEntity>());

        controller.SetActiveTool(new SelectionTool());

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0.2)));

        ToolResult selectionResult = controller.OnPointerReleased(
            new PointerInfo(new Point2D(5, 0.2)));

        Assert.Equal(ToolResultKind.Updated, selectionResult.Kind);
        Assert.True(selectionSet.Contains(createdLine.Id));
        Assert.Equal(1, selectionSet.Count);

        controller.SetActiveTool(
            new MoveTool(),
            cancelCurrentTool: false);

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult moveResult = controller.OnPointerPressed(
            new PointerInfo(new Point2D(3, 4)));

        Assert.Equal(ToolResultKind.Completed, moveResult.Kind);

        var movedLine = (LineEntity)document.Entities.GetRequired(createdLine.Id);

        Assert.Equal(new Point2D(3, 4), movedLine.Start);
        Assert.Equal(new Point2D(13, 4), movedLine.End);

        Assert.True(history.CanUndo);

        history.Undo(document);

        var restoredLine = (LineEntity)document.Entities.GetRequired(createdLine.Id);

        Assert.Equal(new Point2D(0, 0), restoredLine.Start);
        Assert.Equal(new Point2D(10, 0), restoredLine.End);
    }

    [Fact]
    public void ToolRegistryWithToolController_ShouldCreateAndSwitchTools()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        var context = new ToolContext(
            document,
            history,
            new SnapService(),
            selectionSet: selectionSet,
            selectionTolerance: 1);

        var registry = new ToolRegistry();

        var controller = new ToolController(
            context,
            registry.Create(ToolId.Line));

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult lineResult = controller.OnPointerPressed(
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, lineResult.Kind);

        var createdLine = Assert.Single(
            document.Entities.All.OfType<LineEntity>());

        controller.SetActiveTool(
            registry.Create(ToolId.Selection));

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0.2)));

        controller.OnPointerReleased(
            new PointerInfo(new Point2D(5, 0.2)));

        Assert.True(selectionSet.Contains(createdLine.Id));

        controller.SetActiveTool(
            registry.Create(ToolId.Move),
            cancelCurrentTool: false);

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult moveResult = controller.OnPointerPressed(
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Completed, moveResult.Kind);

        var movedLine = (LineEntity)document.Entities.GetRequired(createdLine.Id);

        Assert.Equal(new Point2D(5, 5), movedLine.Start);
        Assert.Equal(new Point2D(15, 5), movedLine.End);
    }
}