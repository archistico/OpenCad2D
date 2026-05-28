using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class PolylineToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new PolylineTool();

        Assert.Equal("Polyline", tool.Name);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, tool.State);
        Assert.Empty(tool.Vertices);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreFirstVertex()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
        Assert.Single(tool.Vertices);
        Assert.Equal(new Point2D(10, 20), tool.Vertices[0]);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterFirstVertex_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(10, 0), tool.CurrentPoint);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.False(preview.IsClosed);
        Assert.Equal(2, preview.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), preview.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), preview.Vertices[1]);
    }

    [Fact]
    public void AdditionalPointerPress_ShouldAddVertexAndUpdateBasePoint()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(2, tool.Vertices.Count);
        Assert.Equal(new Point2D(10, 0), tool.Vertices[1]);
        Assert.Equal(new Point2D(10, 0), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void CompleteOpen_WithTwoVertices_ShouldCreateOpenPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.CompleteOpen(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, tool.State);
        Assert.Empty(tool.Vertices);
        Assert.Null(context.CurrentBasePoint);

        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.False(polyline.IsClosed);
        Assert.Equal(2, polyline.Vertices.Count);
    }

    [Fact]
    public void CompleteClosed_WithThreeVertices_ShouldCreateClosedPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 10)));

        ToolResult result = tool.CompleteClosed(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
    }

    [Fact]
    public void CompleteOpen_WithLessThanTwoVertices_ShouldNotCreateEntity()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.CompleteOpen(context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
    }

    [Fact]
    public void CompleteClosed_WithLessThanThreeVertices_ShouldNotCreateEntity()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.CompleteClosed(context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
    }

    [Fact]
    public void PointerMove_WithOrthoEnabled_ShouldConstrainPreviewFromLastVertex()
    {
        var context = CreateContext();
        context.IsOrthoEnabled = true;
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(16, 8)));

        Assert.Equal(new Point2D(10, 8), tool.CurrentPoint);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(10, 8), preview.Vertices[^1]);
    }

    [Fact]
    public void CompleteOpen_ShouldCreatePolylineOnCurrentLayer()
    {
        CadDocument document = new();
        var layerId = new LayerId("Polylines");

        document.Layers.Add(
            new Layer(
                layerId,
                "Polylines",
                CadColor.FromRgb(255, 0, 0),
                LineWeight.FromMillimeters(0.25)));

        var context = new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            currentLayerId: layerId);

        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.CompleteOpen(context);

        PolylineEntity polyline = Assert.Single(document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(layerId, polyline.LayerId);
    }

    [Fact]
    public void CreatedPolyline_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.CompleteOpen(context);

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void Cancel_ShouldClearVerticesAndBasePoint()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, tool.State);
        Assert.Empty(tool.Vertices);
        Assert.Null(context.CurrentBasePoint);
    }


    [Fact]
    public void GetPromptState_WaitingForFirstPoint_ShouldRequestPoint()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal("POLYLINE", prompt.CommandName);
        Assert.Equal("Specify first point", prompt.Prompt);
        Assert.Equal(CommandInputKind.Point, prompt.ExpectedInput);
        Assert.False(prompt.AcceptsEmptyEnter);
    }

    [Fact]
    public void GetPromptState_CollectingVertices_ShouldExposeCloseAndUndoOptions()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)),
            context);

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal(CommandInputKind.PointOrDistanceOrOption, prompt.ExpectedInput);
        Assert.True(prompt.AcceptsEmptyEnter);
        Assert.Contains(prompt.Options, option => option.Keyword == "Close" && option.Shortcut == "C");
        Assert.Contains(prompt.Options, option => option.Keyword == "Undo" && option.Shortcut == "U");
    }

    [Fact]
    public void HandleCommandInput_WithPointsAndConfirm_ShouldCreateOpenPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)),
            context);
        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)),
            context);
        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.False(polyline.IsClosed);
        Assert.Equal(new[] { new Point2D(0, 0), new Point2D(10, 0) }, polyline.Vertices);
    }

    [Fact]
    public void HandleCommandInput_WithCloseOption_ShouldCreateClosedPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)), context);
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("10,10", new Point2D(10, 10)), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("C", "Close"), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
    }

    [Fact]
    public void HandleCommandInput_WithUndoOption_ShouldRemoveLastVertex()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Single(tool.Vertices);
        Assert.Equal(new Point2D(0, 0), tool.Vertices[0]);
        Assert.Equal(new Point2D(0, 0), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void ToolControllerConfirmActiveToolCommand_WithEnoughVertices_ShouldFinishOpenPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();
        var controller = new ToolController(context, tool);

        controller.OnPointerPressed(new PointerInfo(new Point2D(0, 0)));
        controller.OnPointerPressed(new PointerInfo(new Point2D(10, 0)));

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.False(polyline.IsClosed);
        Assert.Equal(new[] { new Point2D(0, 0), new Point2D(10, 0) }, polyline.Vertices);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, tool.State);
    }

    [Fact]
    public void ToolControllerConfirmActiveToolCommand_WithTooFewVertices_ShouldKeepPolylineActive()
    {
        var context = CreateContext();
        var tool = new PolylineTool();
        var controller = new ToolController(context, tool);

        controller.OnPointerPressed(new PointerInfo(new Point2D(0, 0)));

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Polyline requires at least two points.", result.Message);
        Assert.Empty(context.Document.Entities.All);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
    }



    [Fact]
    public void TryHandleKey_WithAWhileCollectingVertices_ShouldEnterArcMode()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        bool handled = tool.TryHandleKey(context, CadToolKey.A, out ToolResult result);

        Assert.True(handled);
        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForArcPointOnArc, tool.State);
        Assert.Equal("Arc 3P", tool.SegmentMode);
        Assert.Contains("arc mode", result.Message);
    }

    [Fact]
    public void TryHandleKey_WithLWhileArcIsPending_ShouldReturnToLineMode()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.TryHandleKey(context, CadToolKey.A, out _);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));

        bool handled = tool.TryHandleKey(context, CadToolKey.L, out ToolResult result);

        Assert.True(handled);
        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
        Assert.Equal("Line", tool.SegmentMode);
        Assert.Null(tool.ArcPointOnArc);
    }

    [Fact]
    public void GetPromptState_ArcMode_ShouldMakeModeVisibleInCommandName()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.TryHandleKey(context, CadToolKey.A, out _);

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal("POLYLINE ARC", prompt.CommandName);
        Assert.Contains(prompt.Options, option => option.Keyword == "Line" && option.Shortcut == "L");
    }

    [Fact]
    public void GetPromptState_CollectingVertices_ShouldExposeArcOption()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)),
            context);

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Contains(prompt.Options, option => option.Keyword == "Arc" && option.Shortcut == "A");
    }

    [Fact]
    public void HandleCommandInput_WithArcOptionAndThreePointArc_ShouldCreateBulgedPolylineSegment()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        ToolResult arcModeResult = tool.HandleCommandInput(CommandInputSubmission.Option("A", "Arc"), context);
        ToolResult middleResult = tool.HandleCommandInput(CommandInputSubmission.FromPoint("5,5", new Point2D(5, 5)), context);
        ToolResult endResult = tool.HandleCommandInput(CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)), context);
        ToolResult completeResult = tool.CompleteOpen(context);

        Assert.Equal(ToolResultKind.Updated, arcModeResult.Kind);
        Assert.Equal(ToolResultKind.Updated, middleResult.Kind);
        Assert.Equal(ToolResultKind.Updated, endResult.Kind);
        Assert.Equal(ToolResultKind.Completed, completeResult.Kind);

        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(new[] { new Point2D(0, 0), new Point2D(10, 0) }, polyline.Vertices);
        Assert.True(polyline.HasArcSegments);
        double bulge = Assert.Single(polyline.SegmentBulges);
        Assert.Equal(1.0, bulge, precision: 6);
    }

    [Fact]
    public void PointerInput_WithArcOptionAndThreePointArc_ShouldCreateMixedPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "Arc"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(15, 5)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(20, 0)));

        ToolResult result = tool.CompleteOpen(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(3, polyline.Vertices.Count);
        Assert.Equal(2, polyline.SegmentBulges.Count);
        Assert.Equal(0.0, polyline.SegmentBulges[0], precision: 6);
        Assert.Equal(1.0, polyline.SegmentBulges[1], precision: 6);
    }

    [Fact]
    public void GetPreviewEntity_WhileSpecifyingArcEndPoint_ShouldExposeArcBulge()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "Arc"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 0)));

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.True(preview.HasArcSegments);
        double bulge = Assert.Single(preview.SegmentBulges);
        Assert.Equal(1.0, bulge, precision: 6);
    }

    [Fact]
    public void HandleCommandInput_WithArcEndPointBeforeMiddlePoint_ShouldKeepArcInputActive()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "Arc"), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForArcPointOnArc, tool.State);
        Assert.Single(tool.Vertices);
        Assert.Empty(tool.SegmentBulges);
    }

    [Fact]
    public void UndoLastVertex_WhenWaitingForArcEndPoint_ShouldCancelOnlyArcMiddlePoint()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "Arc"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));

        ToolResult result = tool.UndoLastVertex(context);

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForArcPointOnArc, tool.State);
        Assert.Null(tool.ArcPointOnArc);
        Assert.Single(tool.Vertices);
        Assert.Empty(tool.SegmentBulges);
    }

    [Fact]
    public void CompleteOpen_WhileArcSegmentIsIncomplete_ShouldNotCreatePolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("A", "Arc"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));

        ToolResult result = tool.CompleteOpen(context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(context.Document.Entities.All);
        Assert.Equal(PolylineToolState.WaitingForArcEndPoint, tool.State);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
