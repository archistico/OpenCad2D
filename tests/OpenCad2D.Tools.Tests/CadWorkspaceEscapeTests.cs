using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class CadWorkspaceEscapeTests
{
    [Fact]
    public void Escape_WhenSelectionToolIsActiveAndSelectionIsEmpty_ShouldDoNothing()
    {
        var workspace = new CadWorkspace();

        ToolResult result = workspace.Escape();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);
        Assert.True(workspace.SelectionSet.IsEmpty);
    }

    [Fact]
    public void Escape_WhenSelectionToolIsActiveAndSelectionExists_ShouldClearSelection()
    {
        var workspace = new CadWorkspace();
        var line = AddLine(workspace.Document);
        workspace.SelectionSet.Select(line.Id);

        ToolResult result = workspace.Escape();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.True(workspace.SelectionSet.IsEmpty);
        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);
    }

    [Fact]
    public void Escape_WhenLineToolIsActiveBeforeFirstPoint_ShouldReturnToSelection()
    {
        var workspace = new CadWorkspace(initialToolId: ToolId.Line);

        ToolResult result = workspace.Escape();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);
    }

    [Fact]
    public void Escape_WhenLineToolHasFirstPoint_ShouldCancelCommandAndReturnToSelection()
    {
        var workspace = new CadWorkspace(initialToolId: ToolId.Line);
        var lineTool = Assert.IsType<LineTool>(workspace.ToolController.ActiveTool);

        workspace.ToolController.OnPointerPressed(new PointerInfo(new Point2D(0, 0)));

        ToolResult result = workspace.Escape();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, lineTool.State);
        Assert.Null(workspace.Context.CurrentBasePoint);
        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);
        Assert.Equal(0, workspace.Document.Entities.Count);
    }

    [Fact]
    public void Escape_WhenMoveToolIsActive_ShouldReturnToSelectionAndKeepSelection()
    {
        var workspace = new CadWorkspace(initialToolId: ToolId.Move);
        var line = AddLine(workspace.Document);
        workspace.SelectionSet.Select(line.Id);

        workspace.ToolController.OnPointerPressed(new PointerInfo(new Point2D(0, 0)));

        ToolResult result = workspace.Escape();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.True(workspace.SelectionSet.Contains(line.Id));
        Assert.Null(workspace.Context.CurrentBasePoint);
        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);
    }

    [Fact]
    public void Escape_WhenPolylineIsInProgress_ShouldCancelPolylineAndReturnToSelection()
    {
        var workspace = new CadWorkspace(initialToolId: ToolId.Polyline);
        var polylineTool = Assert.IsType<PolylineTool>(workspace.ToolController.ActiveTool);

        workspace.ToolController.OnPointerPressed(new PointerInfo(new Point2D(0, 0)));
        workspace.ToolController.OnPointerPressed(new PointerInfo(new Point2D(10, 0)));

        ToolResult result = workspace.Escape();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, polylineTool.State);
        Assert.Empty(polylineTool.Vertices);
        Assert.Null(workspace.Context.CurrentBasePoint);
        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);
        Assert.Equal(0, workspace.Document.Entities.Count);
    }

    [Fact]
    public void Escape_WhenToolIsCancelled_ShouldRequireSecondEscapeToClearSelection()
    {
        var workspace = new CadWorkspace(initialToolId: ToolId.Move);
        var line = AddLine(workspace.Document);
        workspace.SelectionSet.Select(line.Id);

        workspace.Escape();

        Assert.True(workspace.SelectionSet.Contains(line.Id));
        Assert.IsType<SelectionTool>(workspace.ToolController.ActiveTool);

        workspace.Escape();

        Assert.True(workspace.SelectionSet.IsEmpty);
    }

    private static LineEntity AddLine(CadDocument document)
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        return line;
    }
}
