using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class BoundaryFillToolTests
{
    [Fact]
    public void Constructor_ShouldExposeNameAndGridSnapMode()
    {
        var tool = new BoundaryFillTool();
        ToolContext context = CreateContext();

        Assert.Equal("Boundary Fill", tool.Name);
        Assert.Equal(SnapKind.Grid, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void ClickInsideLineRectangle_ShouldCreatePreviewWithoutAddingEntity()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal("Boundary found — Enter/right-click to confirm", result.Message);
        Assert.True(tool.HasPreview);
        Assert.Equal(4, document.Entities.Count);
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());

        PolylineEntity preview = Assert.Single(tool.GetPreviewEntities(context).OfType<PolylineEntity>());
        Assert.True(preview.IsClosed);
        Assert.True(preview.IsFilled);
        Assert.Equal(4, preview.Vertices.Count);
    }

    [Fact]
    public void ConfirmAfterPreview_ShouldCreateFilledClosedPolyline()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Boundary fill created.", result.Message);
        Assert.False(tool.HasPreview);
        Assert.Equal(5, document.Entities.Count);

        PolylineEntity polyline = Assert.Single(document.Entities.All.OfType<PolylineEntity>());

        Assert.True(polyline.IsClosed);
        Assert.True(polyline.IsFilled);
        Assert.Equal(4, polyline.Vertices.Count);
        Assert.True(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void EnterAfterPreview_ShouldCreateFilledClosedPolyline()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        bool handled = tool.TryHandleKey(
            context,
            CadToolKey.Enter,
            out ToolResult result);

        Assert.True(handled);
        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Single(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void ConfirmAfterPreview_ShouldBeUndoable()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));
        tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        context.CommandHistory.Undo(document);

        Assert.Equal(4, document.Entities.Count);
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void ClickOutsideBoundary_ShouldNotCreatePreviewOrPolyline()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(20, 20)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("No closed boundary was found around the picked point.", result.Message);
        Assert.False(tool.HasPreview);
        Assert.Equal(4, document.Entities.Count);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void CommandInput_ShouldCreatePreviewFromPointAndConfirmShouldCreateBoundaryFill()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult previewResult = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("5,2", new Point2D(5, 2)),
            context);

        Assert.Equal(ToolResultKind.Updated, previewResult.Kind);
        Assert.True(tool.HasPreview);
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());

        ToolResult confirmResult = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, confirmResult.Kind);
        Assert.Single(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void ConfirmWithoutPreview_ShouldNotCreateBoundaryFill()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Pick inside a closed boundary before confirming Boundary Fill.", result.Message);
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void CancelAfterPreview_ShouldClearPreview()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.False(tool.HasPreview);
        Assert.Empty(tool.GetPreviewEntities(context));
        Assert.Equal(4, document.Entities.Count);
    }

    [Fact]
    public void GetPromptState_AfterPreview_ShouldAcceptEmptyEnter()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal("BFILL", prompt.CommandName);
        Assert.Equal(CommandInputKind.Point, prompt.ExpectedInput);
        Assert.True(prompt.AcceptsEmptyEnter);
        Assert.Equal("Boundary found — Enter/right-click to confirm", prompt.Prompt);
    }

    [Fact]
    public void ClickInsideCircle_ShouldPreviewBoundaryUsingCurveSampling()
    {
        var document = new CadDocument();
        document.AddEntity(new CircleEntity(new Point2D(0, 0), 10));
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal("Boundary found from sampled curve(s) — Enter/right-click to confirm", result.Message);
        Assert.True(tool.HasPreview);
        Assert.True(tool.PreviewResult?.Diagnostics.SampledCurveSegmentCount > 0);
    }

    [Fact]
    public void ClickInsideSmallGapRectangle_ShouldPreviewBoundaryAndReportBridgedGap()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        document.AddEntity(new LineEntity(new Point2D(10, 0), new Point2D(10, 5)));
        document.AddEntity(new LineEntity(new Point2D(10, 5), new Point2D(0, 5)));
        document.AddEntity(new LineEntity(new Point2D(0, 5), new Point2D(0, 0.2)));
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal("Boundary found; 1 small gap(s) bridged — Enter/right-click to confirm", result.Message);
        Assert.Equal(1, tool.PreviewResult?.Diagnostics.BridgedGapCount);
    }

    [Fact]
    public void GetPromptState_ShouldExposeBoundaryFillCommand()
    {
        var tool = new BoundaryFillTool();

        CommandPromptState prompt = tool.GetPromptState(CreateContext());

        Assert.Equal("BFILL", prompt.CommandName);
        Assert.Equal(CommandInputKind.Point, prompt.ExpectedInput);
        Assert.False(prompt.AcceptsEmptyEnter);
    }

    private static CadDocument CreateDocumentWithRectangleLines()
    {
        var document = new CadDocument();

        document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        document.AddEntity(new LineEntity(new Point2D(10, 0), new Point2D(10, 5)));
        document.AddEntity(new LineEntity(new Point2D(10, 5), new Point2D(0, 5)));
        document.AddEntity(new LineEntity(new Point2D(0, 5), new Point2D(0, 0)));

        return document;
    }

    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
