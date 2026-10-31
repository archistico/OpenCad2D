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

public sealed class AlignWorkflowRegressionTests
{
    [Fact]
    public void SelectionInsideCommand_ShouldSupportClickShiftAndRequireConfirmation()
    {
        var (context, tool, first) = Prepare(false);
        var second = new LineEntity(new Point2D(0, 10), new Point2D(10, 10));
        context.Document.AddEntity(second);
        Click(tool, context, new Point2D(5, 0));
        Assert.Contains(first.Id, context.Selection.SelectedIds);
        Assert.Equal(AlignToolState.WaitingForEntitySelection, tool.State);
        Click(tool, context, new Point2D(5, 10), PointerModifiers.Shift);
        Assert.Equal(2, context.Selection.SelectedIds.Count);
        Click(tool, context, new Point2D(5, 10), PointerModifiers.Shift);
        Assert.Single(context.Selection.SelectedIds);
        tool.HandleCommandInput(CommandInputSubmission.Confirm(""), context);
        Assert.Equal(AlignToolState.WaitingForSourcePoint1, tool.State);
        Assert.Null(tool.SourcePoint1);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void SelectionInsideCommand_CtrlClick_ShouldCycleOverlappingObjects()
    {
        var (context, tool, first) = Prepare(false);
        var second = new LineEntity(first.Start, first.End, drawOrder: 1);
        context.Document.AddEntity(second);
        Click(tool, context, new Point2D(5, 0));
        Assert.Equal(second.Id, Assert.Single(context.Selection.SelectedIds));
        Click(tool, context, new Point2D(5, 0), PointerModifiers.Control);
        Assert.Equal(first.Id, Assert.Single(context.Selection.SelectedIds));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionInsideCommand_WindowOrCrossing_ShouldReuseSelectionPreviewAndRules(bool crossing)
    {
        var (context, tool, first) = Prepare(false);
        var lockedLayer = new LayerId("locked");
        context.Document.Layers.Add(new Layer(lockedLayer, "locked", isLocked: true));
        context.Document.AddEntity(new LineEntity(new Point2D(0, 1), new Point2D(10, 1), layerId: lockedLayer));
        context.Document.AddEntity(new LineEntity(new Point2D(0, 2), new Point2D(10, 2), isVisible: false));
        var start = new Point2D(crossing ? 11 : -1, -1);
        var end = new Point2D(crossing ? -1 : 11, 3);
        tool.OnPointerPressed(context, new PointerInfo(start));
        tool.OnPointerMoved(context, new PointerInfo(end));
        Assert.Single(tool.GetPreviewDescriptor(context).Windows);
        tool.OnPointerReleased(context, new PointerInfo(end));
        Assert.Equal(first.Id, Assert.Single(context.Selection.SelectedIds));
        Assert.Empty(tool.GetPreviewDescriptor(context).Windows);
    }

    [Fact]
    public void ConfirmEmptySelection_ShouldExplainRecoveryWithoutAdvancing()
    {
        var (context, tool, _) = Prepare(false);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Confirm(""), context);
        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(AlignToolState.WaitingForEntitySelection, tool.State);
        Assert.Contains("Select editable", tool.GetPromptState(context).Feedback);
    }

    [Fact]
    public void SourcePoints_WithEntitySnapEnabled_ShouldUseGeometricPointSnapsOnly()
    {
        var (context, tool, _) = Prepare();
        context.EnabledSnaps = SnapKind.Entity;
        context.SnapTolerance = 2;
        Point2D point = new(5, 0.2);
        Assert.Equal(SnapKind.None, tool.GetActiveSnapKind(context));
        tool.OnPointerPressed(context, new PointerInfo(point));
        Assert.Equal(point, tool.SourcePoint1);
    }

    [Fact]
    public void FirstDestinationHover_ShouldPreviewTranslationAndShowReferencePairWithoutMutation()
    {
        var (context, tool, first) = Prepare();
        Point(tool, context, first.Start);
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(20, 30)));
        ToolPreviewDescriptor preview = tool.GetPreviewDescriptor(context);
        Assert.Equal(new Point2D(20, 30), Assert.IsType<LineEntity>(Assert.Single(preview.Entities)).Start);
        Assert.Equal(ToolPreviewHighlightKind.Addition, preview.EntityHighlightKind);
        Assert.Single(preview.Lines);
        Assert.Equal(2, preview.Markers.Count);
        Assert.Same(first, context.Document.Entities.GetRequired(first.Id));
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidSecondDestination_AfterValidHover_ShouldClearStaleTransform(bool click)
    {
        var (context, tool, _) = Prepare();
        ThreePoints(tool, context);
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(20, 20)));
        Assert.True(tool.HasPreview);
        var invalid = new PointerInfo(new Point2D(20, 0));
        if (click)
            tool.OnPointerPressed(context, invalid);
        else
            tool.OnPointerMoved(context, invalid);
        Assert.False(tool.HasPreview);
        Assert.Null(tool.CurrentTransform);
        Assert.Empty(tool.GetPreviewEntities(context));
        Assert.Contains("destination", tool.GetPromptState(context).Feedback);
        Assert.False(context.CommandHistory.CanUndo);
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(20, 20)));
        Assert.True(tool.HasPreview);
    }

    [Theory]
    [InlineData("remove")]
    [InlineData("replace")]
    [InlineData("hide")]
    [InlineData("lock")]
    [InlineData("selection")]
    public void SourcesChangedBeforeConfirmation_ShouldClearReferencesAndRejectOldTransform(string change)
    {
        var (context, tool, first) = Prepare();
        FourPoints(tool, context);
        switch (change)
        {
            case "remove": context.Document.RemoveEntity(first.Id); break;
            case "replace": context.Document.ReplaceEntity(new LineEntity(first.Start, new Point2D(12, 0), first.Id)); break;
            case "hide": context.Document.ReplaceEntity(new LineEntity(first.Start, first.End, first.Id, isVisible: false)); break;
            case "lock":
                var layer = new LayerId("locked");
                context.Document.Layers.Add(new Layer(layer, "locked", isLocked: true));
                context.Document.ReplaceEntity(new LineEntity(first.Start, first.End, first.Id, layer));
                break;
            case "selection": context.Selection.Set.Clear(); break;
        }
        ToolResult result = tool.ConfirmWithScale(context);
        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(AlignToolState.WaitingForEntitySelection, tool.State);
        Assert.Null(tool.SourcePoint1);
        Assert.False(tool.HasPreview);
        Assert.Contains("Review the selection", tool.GetPromptState(context).Feedback);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void BackFromConfirmation_ShouldAllowCorrectingEveryPointWithoutMutation()
    {
        var (context, tool, first) = Prepare();
        FourPoints(tool, context);
        foreach (var expected in new[] { AlignToolState.WaitingForDestinationPoint2, AlignToolState.WaitingForSourcePoint2,
            AlignToolState.WaitingForDestinationPoint1, AlignToolState.WaitingForSourcePoint1 })
        {
            tool.HandleCommandInput(CommandInputSubmission.Option("B", "Back"), context);
            Assert.Equal(expected, tool.State);
            Assert.Null(tool.CurrentTransform);
        }
        Assert.Null(context.CurrentBasePoint);
        Assert.Equal(first.Id, Assert.Single(context.Selection.SelectedIds));
        Assert.False(context.CommandHistory.CanUndo);
        FourPoints(tool, context);
        tool.HandleCommandInput(CommandInputSubmission.Option("O", "Objects"), context);
        Assert.Equal(AlignToolState.WaitingForEntitySelection, tool.State);
        Assert.False(tool.HasPreview);
        Assert.Empty(tool.GetPreviewDescriptor(context).Markers);
    }

    [Theory]
    [InlineData("unexpected")]
    [InlineData("S")]
    public void UnknownScaleOption_ShouldNeverCommitAsNo(string option)
    {
        var (context, tool, _) = Prepare();
        FourPoints(tool, context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option(option, option), context);
        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(AlignToolState.WaitingForScaleConfirmation, tool.State);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ConfirmWithScale_MultipleNativeEntities_ShouldKeepGeometryTypesAndUndoRedo()
    {
        var (context, tool, first) = Prepare();
        var circle = new CircleEntity(new Point2D(5, 0), 2);
        var polyline = new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(10, 0) }, segmentBulges: new[] { 0.5 });
        context.Document.AddEntity(circle);
        context.Document.AddEntity(polyline);
        context.Selection.Set.SelectMany(new[] { circle.Id, polyline.Id });
        FourPoints(tool, context);
        tool.ConfirmWithScale(context);
        Assert.True(tool.IsCommandFinished);
        var transformedCircle = Assert.IsType<CircleEntity>(context.Document.Entities.GetRequired(circle.Id));
        Assert.Equal(4, transformedCircle.Radius, 8);
        var transformedPolyline = Assert.IsType<PolylineEntity>(context.Document.Entities.GetRequired(polyline.Id));
        Assert.Equal(0.5, Assert.Single(transformedPolyline.SegmentBulges), 8);
        context.CommandHistory.Undo(context.Document);
        Assert.Same(first, context.Document.Entities.GetRequired(first.Id));
        Assert.Same(circle, context.Document.Entities.GetRequired(circle.Id));
        context.CommandHistory.Redo(context.Document);
        Assert.Equal(4, Assert.IsType<CircleEntity>(context.Document.Entities.GetRequired(circle.Id)).Radius, 8);
    }

    private static void ThreePoints(AlignTool tool, ToolContext context)
    {
        Point(tool, context, new Point2D(0, 0));
        Point(tool, context, new Point2D(20, 0));
        Point(tool, context, new Point2D(10, 0));
    }

    private static void FourPoints(AlignTool tool, ToolContext context)
    {
        ThreePoints(tool, context);
        Point(tool, context, new Point2D(20, 20));
    }

    private static void Point(AlignTool tool, ToolContext context, Point2D point) =>
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("", point), context);

    private static void Click(AlignTool tool, ToolContext context, Point2D point, PointerModifiers modifiers = PointerModifiers.None)
    {
        tool.OnPointerPressed(context, new PointerInfo(point, modifiers));
        tool.OnPointerReleased(context, new PointerInfo(point, modifiers));
    }

    private static (ToolContext Context, AlignTool Tool, LineEntity First) Prepare(bool selected = true)
    {
        var context = new ToolContext(new CadDocument(), new CommandHistory(), new SnapService(), selectionTolerance: 0.5);
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        context.Document.AddEntity(line);
        if (selected)
            context.Selection.Set.Select(line.Id);
        return (context, new AlignTool(), line);
    }
}
