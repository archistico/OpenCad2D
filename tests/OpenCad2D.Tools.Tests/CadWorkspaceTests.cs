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
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class CadWorkspaceTests
{
    [Fact]
    public void Constructor_ShouldCreateDefaultServices()
    {
        var workspace = new CadWorkspace();

        Assert.NotNull(workspace.Document);
        Assert.NotNull(workspace.CommandHistory);
        Assert.NotNull(workspace.SelectionSet);
        Assert.NotNull(workspace.SnapService);
        Assert.NotNull(workspace.SelectionService);
        Assert.NotNull(workspace.ToolRegistry);
        Assert.NotNull(workspace.Context);
        Assert.NotNull(workspace.ToolController);
        Assert.NotNull(workspace.ActionController);

        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);
    }

    [Fact]
    public void Constructor_ShouldUseProvidedServices()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();
        SnapService snapService = new();
        SelectionService selectionService = new();
        ToolRegistry registry = new();

        var workspace = new CadWorkspace(
            document,
            history,
            selectionSet,
            snapService,
            selectionService,
            registry);

        Assert.Same(document, workspace.Document);
        Assert.Same(history, workspace.CommandHistory);
        Assert.Same(selectionSet, workspace.SelectionSet);
        Assert.Same(snapService, workspace.SnapService);
        Assert.Same(selectionService, workspace.SelectionService);
        Assert.Same(registry, workspace.ToolRegistry);
    }

    [Fact]
    public void Constructor_ShouldConfigureToolContext()
    {
        var workspace = new CadWorkspace(
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 3,
            selectionTolerance: 2,
            selectionDragThreshold: 4);

        Assert.Equal(SnapKind.Endpoint, workspace.Context.EnabledSnaps);
        Assert.Equal(3, workspace.Context.SnapTolerance);
        Assert.Equal(2, workspace.Context.SelectionTolerance);
        Assert.Equal(4, workspace.Context.SelectionDragThreshold);
    }

    [Fact]
    public void Constructor_WithInitialToolId_ShouldSetActiveTool()
    {
        var workspace = new CadWorkspace(
            initialToolId: ToolId.Line);

        Assert.IsType<LineTool>(workspace.ToolController.ActiveTool);
        Assert.Equal("Line", workspace.ToolController.ActiveToolName);
    }

    [Fact]
    public void SetActiveTool_ShouldChangeActiveTool()
    {
        var workspace = new CadWorkspace();

        ToolResult result = workspace.SetActiveTool(ToolId.Rectangle);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.IsType<RectangleTool>(workspace.ToolController.ActiveTool);
    }

    [Fact]
    public void SetActiveTool_ShouldDeactivatePreviousTool()
    {
        var workspace = new CadWorkspace(
            initialToolId: ToolId.Line);

        var lineTool = Assert.IsType<LineTool>(
            workspace.ToolController.ActiveTool);

        workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(
            TwoPointToolState.WaitingForSecondPoint,
            lineTool.State);

        workspace.SetActiveTool(ToolId.Selection);

        Assert.Equal(
            TwoPointToolState.WaitingForFirstPoint,
            lineTool.State);

        Assert.IsType<SelectionTool>(
            workspace.ToolController.ActiveTool);
    }

    [Fact]
    public void Workspace_ShouldDrawLineThroughToolController()
    {
        var workspace = new CadWorkspace(
            initialToolId: ToolId.Line);

        workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, workspace.Document.Entities.Count);

        var line = Assert.Single(
            workspace.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    [Fact]
    public void Workspace_ShouldSelectMoveAndUndo()
    {
        var workspace = new CadWorkspace(
            selectionTolerance: 1);

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        workspace.Document.AddEntity(line);

        workspace.SetActiveTool(ToolId.Selection);

        workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0.2)));

        workspace.ToolController.OnPointerReleased(
            new PointerInfo(new Point2D(5, 0.2)));

        Assert.True(workspace.SelectionSet.Contains(line.Id));

        workspace.SetActiveTool(ToolId.Move);

        workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        ToolResult moveResult = workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, moveResult.Kind);

        var moved = (LineEntity)workspace.Document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 2), moved.Start);
        Assert.Equal(new Point2D(15, 2), moved.End);

        ToolResult undoResult = workspace.ActionController.Undo();

        Assert.Equal(ToolResultKind.Completed, undoResult.Kind);

        var restored = (LineEntity)workspace.Document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(10, 0), restored.End);
    }

    [Fact]
    public void Workspace_ShouldDeleteSelectionThroughActionController()
    {
        var workspace = new CadWorkspace();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        workspace.Document.AddEntity(line);
        workspace.SelectionSet.Select(line.Id);

        ToolResult result = workspace.ActionController.DeleteSelection();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, workspace.Document.Entities.Count);
        Assert.True(workspace.SelectionSet.IsEmpty);

        workspace.ActionController.Undo();

        Assert.Equal(1, workspace.Document.Entities.Count);
        Assert.True(workspace.Document.Entities.Contains(line.Id));
    }

    [Fact]
    public void SetActiveToolWithoutDeactivating_ShouldChangeToolWithoutResettingPrevious()
    {
        var workspace = new CadWorkspace(
            initialToolId: ToolId.Line);

        var lineTool = Assert.IsType<LineTool>(
            workspace.ToolController.ActiveTool);

        workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(
            TwoPointToolState.WaitingForSecondPoint,
            lineTool.State);

        workspace.SetActiveToolWithoutDeactivating(ToolId.Selection);

        Assert.Equal(
            TwoPointToolState.WaitingForSecondPoint,
            lineTool.State);

        Assert.IsType<SelectionTool>(
            workspace.ToolController.ActiveTool);
    }

    [Fact]
    public void Constructor_ShouldConfigureGridSettings()
    {
        var gridSettings = new GridSettings(
            step: 25,
            originX: 5,
            originY: 10);

        var workspace = new CadWorkspace(
            gridSettings: gridSettings);

        Assert.Same(gridSettings, workspace.GridSettings);
        Assert.Same(gridSettings, workspace.Context.GridSettings);
    }

    [Fact]
    public void Constructor_ShouldConfigureCurrentLayer()
    {
        var layerId = new LayerId("Walls");

        var workspace = new CadWorkspace(
            currentLayerId: layerId);

        Assert.Equal(layerId, workspace.CurrentLayerId);
        Assert.Equal(layerId, workspace.Context.CurrentLayerId);
    }


    [Fact]
    public void AssignSelectedEntitiesToCurrentLayer_WhenSelectionIsEmpty_ShouldDoNothing()
    {
        var workspace = new CadWorkspace();

        ToolResult result = workspace.AssignSelectedEntitiesToCurrentLayer();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(workspace.Document.Entities.All);
    }

    [Fact]
    public void AssignSelectedEntitiesToCurrentLayer_ShouldMoveSelectedEntitiesToCurrentLayer()
    {
        var targetLayerId = new LayerId("Walls");
        var workspace = new CadWorkspace();
        workspace.Document.Layers.Add(new Layer(targetLayerId, "Walls"));
        workspace.CurrentLayerId = targetLayerId;

        var selectedLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: LayerId.Default);
        var unselectedLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1),
            layerId: LayerId.Default);

        workspace.Document.AddEntity(selectedLine);
        workspace.Document.AddEntity(unselectedLine);
        workspace.SelectionSet.Select(selectedLine.Id);

        ToolResult result = workspace.AssignSelectedEntitiesToCurrentLayer();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(
            targetLayerId,
            workspace.Document.Entities.GetRequired(selectedLine.Id).LayerId);
        Assert.Equal(
            LayerId.Default,
            workspace.Document.Entities.GetRequired(unselectedLine.Id).LayerId);
        Assert.True(workspace.SelectionSet.Contains(selectedLine.Id));
    }

    [Fact]
    public void AssignSelectedEntitiesToCurrentLayer_ShouldBeUndoable()
    {
        var targetLayerId = new LayerId("Walls");
        var workspace = new CadWorkspace();
        workspace.Document.Layers.Add(new Layer(targetLayerId, "Walls"));
        workspace.CurrentLayerId = targetLayerId;

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: LayerId.Default);

        workspace.Document.AddEntity(line);
        workspace.SelectionSet.Select(line.Id);

        workspace.AssignSelectedEntitiesToCurrentLayer();

        workspace.ActionController.Undo();

        Assert.Equal(
            LayerId.Default,
            workspace.Document.Entities.GetRequired(line.Id).LayerId);

        workspace.ActionController.Redo();

        Assert.Equal(
            targetLayerId,
            workspace.Document.Entities.GetRequired(line.Id).LayerId);
    }

    [Fact]
    public void AssignSelectedEntitiesToCurrentLayer_WhenAllSelectedEntitiesAreAlreadyOnCurrentLayer_ShouldDoNothing()
    {
        var workspace = new CadWorkspace();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: LayerId.Default);

        workspace.Document.AddEntity(line);
        workspace.SelectionSet.Select(line.Id);

        ToolResult result = workspace.AssignSelectedEntitiesToCurrentLayer();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, workspace.CommandHistory.UndoCount);
    }



    [Fact]
    public void ApplyLayerChanges_UndoAndRedo_ShouldRestoreCurrentLayer()
    {
        var wallsLayerId = new LayerId("Walls");
        var doorsLayerId = new LayerId("Doors");
        var workspace = new CadWorkspace();

        workspace.Document.Layers.Add(new Layer(wallsLayerId, "Walls"));
        workspace.Document.Layers.Add(new Layer(doorsLayerId, "Doors"));
        workspace.CurrentLayerId = wallsLayerId;

        List<Layer> nextLayers = workspace.Document.Layers.All.ToList();

        ToolResult result = workspace.ApplyLayerChanges(
            nextLayers,
            doorsLayerId);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(doorsLayerId, workspace.CurrentLayerId);

        workspace.ActionController.Undo();

        Assert.Equal(wallsLayerId, workspace.CurrentLayerId);

        workspace.ActionController.Redo();

        Assert.Equal(doorsLayerId, workspace.CurrentLayerId);
    }

    [Fact]
    public void SetCurrentLayerLocked_ShouldBeUndoableAndMarkDocumentDirty()
    {
        var workspace = new CadWorkspace();

        Assert.False(workspace.IsDirty);

        ToolResult result = workspace.SetCurrentLayerLocked(true);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.True(workspace.Document.Layers.GetRequired(LayerId.Default).IsLocked);
        Assert.True(workspace.IsDirty);
        Assert.True(workspace.CommandHistory.CanUndo);

        workspace.ActionController.Undo();

        Assert.False(workspace.Document.Layers.GetRequired(LayerId.Default).IsLocked);
        Assert.False(workspace.IsDirty);

        workspace.ActionController.Redo();

        Assert.True(workspace.Document.Layers.GetRequired(LayerId.Default).IsLocked);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void SetCurrentLayerVisibility_ShouldBeUndoableAndMarkDocumentDirty()
    {
        var workspace = new CadWorkspace();

        workspace.Document.Layers.SetVisibility(
            LayerId.Default,
            false);
        workspace.MarkSaved();

        ToolResult result = workspace.SetCurrentLayerVisibility(true);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.True(workspace.Document.Layers.GetRequired(LayerId.Default).IsVisible);
        Assert.True(workspace.IsDirty);
        Assert.True(workspace.CommandHistory.CanUndo);

        workspace.ActionController.Undo();

        Assert.False(workspace.Document.Layers.GetRequired(LayerId.Default).IsVisible);
        Assert.False(workspace.IsDirty);

        workspace.ActionController.Redo();

        Assert.True(workspace.Document.Layers.GetRequired(LayerId.Default).IsVisible);
        Assert.True(workspace.IsDirty);
    }

}
