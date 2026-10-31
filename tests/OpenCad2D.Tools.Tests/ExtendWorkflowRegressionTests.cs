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

namespace OpenCad2D.Tools.Tests;

public sealed class ExtendWorkflowRegressionTests
{
    [Fact]
    public void Confirm_WithoutBoundary_ShouldUseAllAndThenFinishWithoutMutation()
    {
        var (context, tool, _, _) = Prepare();
        tool.HandleCommandInput(CommandInputSubmission.Option("E", "Edges"), context);
        tool.HandleCommandInput(CommandInputSubmission.Confirm(""), context);
        Assert.Equal(ExtendToolState.WaitingForTargetEntity, tool.State);
        Assert.Contains("all visible", tool.GetPromptState(context).Prompt);
        tool.HandleCommandInput(CommandInputSubmission.Confirm(""), context);
        Assert.True(tool.IsCommandFinished);
        Assert.False(context.CommandHistory.CanUndo);
        Assert.Empty(tool.GetPreviewDescriptor(context).EntityOverlays);
    }

    [Fact]
    public void CtrlBoundary_WithMultipleEdges_ShouldStopAtNearestEdgeAndKeepPreviewCommitAgreement()
    {
        var (context, tool, _, target) = Prepare();
        var nearer = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));
        context.Document.AddEntity(nearer);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(7, 2), PointerModifiers.Control));
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        LineEntity preview = Assert.IsType<LineEntity>(Assert.Single(tool.GetPreviewEntities()));
        tool.OnPointerPressed(context, new PointerInfo(target.End));
        LineEntity actual = Assert.IsType<LineEntity>(context.Document.Entities.GetRequired(target.Id));
        Assert.Equal(new Point2D(7, 0), actual.End);
        Assert.Equal(preview.End, actual.End);
        Assert.False(tool.IsCommandFinished);
    }

    [Fact]
    public void CtrlHover_WithValidPreview_ShouldClearAdditionAndExplainAddingBoundary()
    {
        var (context, tool, _, target) = Prepare();
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 2), PointerModifiers.Control));
        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);
        Assert.Empty(descriptor.ReplacedEntityIds);
        Assert.Empty(descriptor.HighlightedEntities);
        Assert.False(tool.HasPreview);
        Assert.Contains("Ctrl-click", tool.GetPromptState(context).Feedback);
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        Assert.True(tool.HasPreview);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BoundaryRemovedOrHidden_WithExistingPreview_ShouldClearGhostAndAllowRecovery(bool hide)
    {
        var (context, tool, boundary, target) = Prepare();
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        if (hide)
            context.Document.ReplaceEntity(new LineEntity(boundary.Start, boundary.End, boundary.Id, isVisible: false));
        else
            context.Document.RemoveEntity(boundary.Id);
        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);
        Assert.True(descriptor.IsEmpty);
        Assert.False(tool.HasPreview);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(target.End));
        Assert.Contains("No visible boundaries", result.Message);
        Assert.Same(target, context.Document.Entities.GetRequired(target.Id));
        var replacement = new LineEntity(new Point2D(8, -5), new Point2D(8, 5));
        context.Document.AddEntity(replacement);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 2), PointerModifiers.Control));
        Assert.Equal(ToolResultKind.Completed, tool.OnPointerPressed(context, new PointerInfo(target.End)).Kind);
    }

    [Fact]
    public void BoundaryReplaced_AfterSelection_ShouldUseCurrentGeometry()
    {
        var (context, tool, boundary, target) = Prepare();
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        context.Document.ReplaceEntity(new LineEntity(new Point2D(12, -5), new Point2D(12, 5), boundary.Id));
        Assert.False(tool.GetPreviewDescriptor(context).ReplacedEntityIds.Any());
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        Assert.Equal(new Point2D(12, 0), Assert.IsType<LineEntity>(Assert.Single(tool.GetPreviewEntities())).End);
        tool.OnPointerPressed(context, new PointerInfo(target.End));
        Assert.Equal(new Point2D(12, 0), Assert.IsType<LineEntity>(context.Document.Entities.GetRequired(target.Id)).End);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TargetReplaced_WithPreview_ShouldClearTransientGeometryAndRespectLock(bool locked)
    {
        var (context, tool, _, target) = Prepare();
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        var layerId = new LayerId("replacement");
        context.Document.Layers.Add(new Layer(layerId, "replacement", isLocked: locked));
        var replacement = new LineEntity(target.Start, new Point2D(6, 0), target.Id, layerId);
        context.Document.ReplaceEntity(replacement);
        Assert.Empty(tool.GetPreviewDescriptor(context).ReplacedEntityIds);
        if (locked)
        {
            ToolResult result = tool.OnPointerPressed(context, new PointerInfo(replacement.End));
            Assert.Contains("not editable", result.Message);
            Assert.Same(replacement, context.Document.Entities.GetRequired(target.Id));
        }
        else
        {
            tool.OnPointerMoved(context, new PointerInfo(replacement.End));
            Assert.Equal(new Point2D(6, 0), Assert.IsType<LineEntity>(Assert.Single(tool.GetHighlightedPreviewEntities())).Start);
        }
    }

    [Fact]
    public void TargetNearSelectedBoundary_ShouldPreferSupportedTarget()
    {
        var (context, tool, _, target) = Prepare();
        var boundary = new LineEntity(new Point2D(0, 0.1), new Point2D(10, -0.05));
        context.Document.AddEntity(boundary);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(9, -0.035), PointerModifiers.Control));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(4, 0.02)));
        Assert.True(tool.HasPreview);
        Assert.Equal(target.Id, Assert.Single(tool.GetPreviewDescriptor(context).ReplacedEntityIds));
    }

    [Fact]
    public void FailedClick_AfterValidHover_ShouldClearPreviewAndExposeSameFeedbackAsHover()
    {
        var (context, tool, _, target) = Prepare();
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        Point2D middle = new(2.5, 0);
        ToolResult click = tool.OnPointerPressed(context, new PointerInfo(middle));
        Assert.False(tool.HasPreview);
        Assert.Empty(tool.GetPreviewDescriptor(context).ReplacedEntityIds);
        ToolResult hover = tool.OnPointerMoved(context, new PointerInfo(middle));
        Assert.Equal(click.Message, hover.Message);
        Assert.Contains("ambiguous", tool.GetPromptState(context).Feedback);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void EdgesAfterExtend_ShouldKeepLocalUndoAndRedo()
    {
        var (context, tool, _, target) = Prepare();
        tool.OnPointerPressed(context, new PointerInfo(target.End));
        tool.HandleCommandInput(CommandInputSubmission.Option("E", "Edges"), context);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
        tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);
        Assert.Same(target, context.Document.Entities.GetRequired(target.Id));
        context.CommandHistory.Redo(context.Document);
        Assert.Equal(new Point2D(10, 0), Assert.IsType<LineEntity>(context.Document.Entities.GetRequired(target.Id)).End);
        tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);
        Assert.Same(target, context.Document.Entities.GetRequired(target.Id));
        Assert.Equal(0, context.CommandHistory.CurrentGeneration);
    }

    [Fact]
    public void LocalUndo_WhenAnotherActionIsNext_ShouldLeaveHistoryAndDocumentUnchanged()
    {
        var (context, tool, _, target) = Prepare();
        tool.OnPointerPressed(context, new PointerInfo(target.End));
        var unrelated = new LineEntity(new Point2D(20, 0), new Point2D(20, 5));
        context.Commands.Execute(context.Document, new AddEntityCommand(unrelated));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);
        Assert.Contains("will not undo other", result.Message);
        Assert.True(context.Document.Entities.Contains(unrelated.Id));
        Assert.Equal(2, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void AllMode_AfterNewBoundaryAppears_ShouldRefreshAndExcludeTargetFromRedOverlay()
    {
        var (context, tool, _, target) = Prepare();
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "All"), context);
        var near = new LineEntity(new Point2D(7, -5), new Point2D(7, 5), isLocked: true);
        context.Document.AddEntity(near);
        tool.OnPointerMoved(context, new PointerInfo(target.End));
        var descriptor = tool.GetPreviewDescriptor(context);
        Assert.DoesNotContain(descriptor.EntityOverlays.SelectMany(overlay => overlay.Entities), entity => entity.Id == target.Id);
        Assert.Contains(descriptor.EntityOverlays.SelectMany(overlay => overlay.Entities), entity => entity.Id == near.Id);
        Assert.Equal(new Point2D(7, 0), Assert.IsType<LineEntity>(Assert.Single(tool.GetPreviewEntities())).End);
    }

    private static (ToolContext Context, ExtendTool Tool, LineEntity Boundary, LineEntity Target) Prepare()
    {
        var context = new ToolContext(new CadDocument(), new CommandHistory(), new SnapService(), selectionTolerance: 0.5);
        var boundary = new LineEntity(new Point2D(10, -5), new Point2D(10, 5));
        var target = new LineEntity(new Point2D(0, 0), new Point2D(5, 0));
        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);
        var tool = new ExtendTool();
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 2)));
        return (context, tool, boundary, target);
    }
}
