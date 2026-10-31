using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class TrimToolRegressionTests
{
    [Fact]
    public void TargetPick_OnIntersection_ShouldKeepDocumentAndExplainWhereToClick()
    {
        ToolContext context = CreateContext(out _, out LineEntity target);
        var tool = SelectBoundary(context);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("on a cutting intersection", result.Message);
        Assert.Contains("Click inside", tool.GetPromptState(context).Feedback);
        Assert.True(context.Document.Entities.Contains(target.Id));
        Assert.Equal(1, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void ClosedTarget_WithOnlyTangentialContact_ShouldExplainMissingSecondIntersection()
    {
        var context = new ToolContext(new CadDocument(), new CommandHistory(), new SnapService(), selectionTolerance: 0.5);
        context.Document.AddEntity(new CircleEntity(Point2D.Origin, 5));
        context.Document.AddEntity(new LineEntity(new Point2D(-10, 5), new Point2D(10, 5)));
        var tool = new TrimTool();
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 5)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(5, 0)));

        Assert.False(tool.HasPreview);
        Assert.Contains("two distinct cutting intersections", result.Message);
    }

    [Fact]
    public void ExplicitBoundary_WhenReplaced_ShouldUseCurrentGeometry()
    {
        ToolContext context = CreateContext(out LineEntity boundary, out _);
        var tool = SelectBoundary(context);
        context.Document.ReplaceEntity(
            new LineEntity(new Point2D(7, -5), new Point2D(7, 5), id: boundary.Id));

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(9, 0)));

        Assert.Contains(context.Document.Entities.All.OfType<LineEntity>(),
            line => line.Start == new Point2D(0, 0) && line.End == new Point2D(7, 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Boundary_WhenHidden_ShouldNotCutOrRemainHighlighted(bool all)
    {
        ToolContext context = CreateContext(out LineEntity boundary, out LineEntity target);
        TrimTool tool = all ? new TrimTool() : SelectBoundary(context);
        if (all) tool.HandleCommandInput(CommandInputSubmission.Option("A", "All"), context);
        context.Document.ReplaceEntity(
            new LineEntity(boundary.Start, boundary.End, id: boundary.Id, isVisible: false));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(context.Document.Entities.Contains(target.Id));
        Assert.DoesNotContain(tool.GetPreviewDescriptor(context).EntityOverlays.SelectMany(overlay => overlay.Entities),
            entity => entity.Id == boundary.Id);
    }

    [Fact]
    public void Boundary_OnLockedLayer_ShouldRemainUsableWithoutBeingModified()
    {
        ToolContext context = CreateContext(out LineEntity boundary, out LineEntity target);
        var layer = new Layer(new LayerId("Locked"), "Locked cutting edges", isLocked: true);
        context.Document.Layers.Add(layer);
        context.Document.ReplaceEntity(
            new LineEntity(boundary.Start, boundary.End, id: boundary.Id, layerId: layer.Id));
        var tool = SelectBoundary(context);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(target.Id));
        Assert.True(context.Document.Entities.Contains(boundary.Id));
    }

    [Fact]
    public void Target_OnLockedLayer_ShouldNotBeModified()
    {
        ToolContext context = CreateContext(out _, out LineEntity target);
        var layer = new Layer(new LayerId("Locked"), "Locked target", isLocked: true);
        context.Document.Layers.Add(layer);
        context.Document.ReplaceEntity(
            new LineEntity(target.Start, target.End, id: target.Id, layerId: layer.Id));
        var tool = SelectBoundary(context);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(context.Document.Entities.Contains(target.Id));
        Assert.Equal(1, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Undo_AfterGlobalUndoAndRedo_ShouldStillUndoOwnedTrim()
    {
        ToolContext context = CreateContext(out _, out LineEntity target);
        var tool = SelectBoundary(context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 0)));
        context.CommandHistory.Undo(context.Document);
        context.CommandHistory.Redo(context.Document);

        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void TargetPick_NearCuttingEdge_ShouldSelectTargetBehindBoundary()
    {
        ToolContext context = CreateContext(out _, out LineEntity target);
        var tool = SelectBoundary(context);

        tool.OnPointerMoved(context, new PointerInfo(new Point2D(5.1, 0.2)));
        Assert.True(tool.HasPreview);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5.1, 0.2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(target.Id));
        Assert.Contains(context.Document.Entities.All.OfType<LineEntity>(),
            line => line.Start == new Point2D(0, 0) && line.End == new Point2D(5, 0));
    }

    [Fact]
    public void All_AfterTrim_ShouldHighlightCurrentGeometryOnly()
    {
        ToolContext context = CreateContext(out _, out LineEntity target);
        var tool = new TrimTool();
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "All"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 0)));

        CadEntity[] emphasized = tool.GetPreviewDescriptor(context).EntityOverlays
            .SelectMany(overlay => overlay.Entities).ToArray();

        Assert.DoesNotContain(emphasized, entity => entity.Id == target.Id);
        Assert.All(emphasized, entity => Assert.True(context.Document.Entities.Contains(entity.Id)));
        Assert.Equal(context.Document.Entities.Count, emphasized.Length);
    }

    [Fact]
    public void All_WhenNewVisibleBoundaryIsAdded_ShouldUseItWithoutRestarting()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = new TrimTool();
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "All"), context);
        var newBoundary = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));
        context.Document.AddEntity(newBoundary);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(6, 0)));

        LineEntity[] horizontal = context.Document.Entities.All.OfType<LineEntity>()
            .Where(line => line.Start.Y == 0 && line.End.Y == 0).ToArray();
        Assert.Equal(2, horizontal.Length);
        Assert.Contains(horizontal, line => line.End == new Point2D(5, 0));
        Assert.Contains(horizontal, line => line.Start == new Point2D(7, 0));
    }

    [Fact]
    public void Undo_WhenAnotherCommandIsOnTop_ShouldNotUndoUnrelatedAction()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = SelectBoundary(context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 0)));
        var point = new PointEntity(new Point2D(100, 100));
        context.Commands.Execute(context.Document, new AddEntityCommand(point));
        int undoCount = context.CommandHistory.UndoCount;

        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(context.Document.Entities.Contains(point.Id));
        Assert.Equal(undoCount, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Undo_AfterGlobalUndo_ShouldNotUndoPreexistingDrawing()
    {
        ToolContext context = CreateContext(out LineEntity boundary, out LineEntity target);
        var tool = SelectBoundary(context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 0)));
        context.CommandHistory.Undo(context.Document);

        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(context.Document.Entities.Contains(boundary.Id));
        Assert.True(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void ControlHover_WhileAddingBoundary_ShouldNotShowRemovalPreview()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = SelectBoundary(context);
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(8, 0)));
        Assert.True(tool.HasPreview);

        ToolResult result = tool.OnPointerMoved(context,
            new PointerInfo(new Point2D(8, 0), PointerModifiers.Control));

        Assert.False(tool.HasPreview);
        Assert.Empty(tool.GetHighlightedPreviewEntities());
        Assert.Contains("cutting edge", result.Message);
    }

    [Fact]
    public void Confirm_BeforeSelectingBoundary_ShouldUseAllWithoutModifyingDocument()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = new TrimTool();

        Assert.True(tool.GetPromptState(context).AcceptsEmptyEnter);
        tool.HandleCommandInput(CommandInputSubmission.Confirm(string.Empty), context);

        Assert.Equal(TrimToolState.WaitingForTargetEntity, tool.State);
        Assert.Equal(1, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Confirm_AfterBoundarySelection_ShouldReturnToSelectionTool()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = SelectBoundary(context);
        var controller = new ToolController(context, tool);

        controller.ConfirmActiveToolCommand();

        Assert.IsType<SelectionTool>(controller.ActiveTool);
    }

    private static TrimTool SelectBoundary(ToolContext context)
    {
        var tool = new TrimTool();
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, -3)));
        return tool;
    }

    private static ToolContext CreateContext(out LineEntity boundary, out LineEntity target)
    {
        var context = new ToolContext(new CadDocument(), new CommandHistory(), new SnapService(), selectionTolerance: 0.5);
        boundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));
        target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        context.Commands.Execute(context.Document, new AddEntityCommand(new CadEntity[] { boundary, target }));
        return context;
    }
}
