using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class FilletToolTests
{
    [Fact]
    public void RadiusOption_ShouldPromptForRadiusAndUpdateRadius()
    {
        var context = CreateContext();
        var tool = new FilletTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("R", "Radius"),
            context);
        ToolResult radiusResult = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3", 3),
            context);

        Assert.Equal(ToolResultKind.Started, optionResult.Kind);
        Assert.Equal(ToolResultKind.Started, radiusResult.Kind);
        Assert.Equal(3, tool.Radius);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
    }

    [Fact]
    public void LineLine_WithZeroRadius_ShouldJoinLinesAtIntersection()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.Count());
        Assert.All(document.Entities.All, entity => Assert.IsType<LineEntity>(entity));
    }

    [Fact]
    public void OnPointerMoved_AfterFirstLine_ShouldExposeFilletPreviewWithoutChangingDocument()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        Assert.Equal(3, preview.Count);
        Assert.Contains(preview, entity => entity is ArcEntity);
        Assert.Equal(2, document.Entities.All.Count());
    }

    [Fact]
    public void CompletedFillet_ShouldClearPreviewEntities()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Empty(tool.GetPreviewEntities());
    }


    [Fact]
    public void TrimOption_ShouldPromptForTrimModeAndSetNoTrim()
    {
        var context = CreateContext();
        var tool = new FilletTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("T", "Trim"),
            context);
        ToolResult modeResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("N", "NoTrim"),
            context);

        Assert.Equal(ToolResultKind.Started, optionResult.Kind);
        Assert.Equal(ToolResultKind.Started, modeResult.Kind);
        Assert.False(tool.TrimEnabled);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
    }

    [Fact]
    public void LineLine_WithNoTrim_ShouldAddFilletArcAndKeepOriginalLines()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        Assert.Contains(document.Entities.All, entity => ReferenceEquals(entity, horizontal));
        Assert.Contains(document.Entities.All, entity => ReferenceEquals(entity, vertical));
        Assert.Contains(document.Entities.All, entity => entity is ArcEntity);
    }

    [Fact]
    public void OnPointerMoved_WithNoTrim_ShouldPreviewOnlyFilletArc()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        Assert.Single(preview);
        Assert.IsType<ArcEntity>(preview[0]);
    }

    [Fact]
    public void LineLine_WithNoTrimAndZeroRadius_ShouldNotModifyDocument()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(2, document.Entities.All.Count());
    }

    [Fact]
    public void LineLine_WithNearlyParallelLines_ShouldNotThrowOrModifyDocument()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(1000, 0));
        var second = new LineEntity(new Point2D(0, 1), new Point2D(1000, 1.000001));
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(500, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(500, 1)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(2, document.Entities.All.Count());
    }



    [Fact]
    public void PolylineAdjacentSegments_WithRadius_ShouldCreateSingleBulgedPolyline()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity filleted = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All));
        Assert.Equal(4, filleted.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), filleted.Vertices[0]);
        Assert.Equal(8.0, filleted.Vertices[1].X, 12);
        Assert.Equal(0.0, filleted.Vertices[1].Y, 12);
        Assert.Equal(10.0, filleted.Vertices[2].X, 12);
        Assert.Equal(2.0, filleted.Vertices[2].Y, 12);
        Assert.Equal(new Point2D(10, 10), filleted.Vertices[3]);
        Assert.Equal(3, filleted.SegmentBulges.Count);
        Assert.Equal(0.0, filleted.SegmentBulges[0], 12);
        Assert.True(filleted.SegmentBulges[1] < 0.0);
        Assert.Equal(0.0, filleted.SegmentBulges[2], 12);
    }


    [Fact]
    public void PolylineAdjacentSegments_WithObtuseCorner_ShouldCreateArcWithRequestedRadius()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(15, 10)
        });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(12, 4)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity filleted = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All));
        Assert.Equal(4, filleted.Vertices.Count);

        double bulge = filleted.SegmentBulges[1];
        Assert.NotEqual(0.0, bulge);

        double chordLength = filleted.Vertices[1].DistanceTo(filleted.Vertices[2]);
        double sweep = Math.Abs(-4.0 * Math.Atan(bulge));
        double actualRadius = chordLength / (2.0 * Math.Sin(sweep / 2.0));

        Assert.Equal(2.0, actualRadius, 10);
    }

    [Fact]
    public void PolylineAdjacentSegments_WithUndo_ShouldRestoreOriginalPolyline()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        var history = new CommandHistory();
        document.AddEntity(polyline);
        ToolContext context = new(
            document,
            history,
            new SnapService(),
            selectionTolerance: 5);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        history.Undo(document);

        Assert.Same(polyline, Assert.Single(document.Entities.All));
    }

    [Fact]
    public void PolylineNonAdjacentSegments_ShouldReturnClearErrorAndNotModifyDocument()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(20, 10)
        });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(15, 10)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Selected polyline segments are not adjacent.", result.Message);
        Assert.Same(polyline, Assert.Single(document.Entities.All));
    }

    [Fact]
    public void PolylineWithExistingBulge_ShouldReturnClearErrorAndNotModifyDocument()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            segmentBulges: new[] { -0.25, 0.0 });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Polyline segment fillet currently supports linear polylines only.", result.Message);
        Assert.Same(polyline, Assert.Single(document.Entities.All));
    }

    [Fact]
    public void GetActiveSnapKind_WhenSelectingEntities_ShouldUseEntityOnlySnap()
    {
        var context = CreateContext();
        var tool = new FilletTool();

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));

        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        context.Document.AddEntity(first);
        context.Document.AddEntity(second);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void GetPreviewDescriptor_AfterFirstLine_ShouldHighlightSelectedFirstEntity()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);
        ToolPreviewEntityOverlay overlay = Assert.Single(descriptor.EntityOverlays);

        Assert.Equal(ToolPreviewHighlightKind.Emphasis, overlay.Kind);
        Assert.Same(horizontal, Assert.Single(overlay.Entities));
    }


    [Fact]
    public void ConfirmAtRadiusPrompt_ShouldKeepCurrentRadiusAndReturnToEntitySelection()
    {
        var context = CreateContext();
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
        Assert.Equal(0, tool.Radius);
        Assert.Equal("Fillet radius remains 0. Select first line.", result.Message);
    }

    [Fact]
    public void ToolControllerConfirmActiveToolCommand_AtRadiusPrompt_ShouldKeepCurrentRadius()
    {
        var context = CreateContext();
        var tool = new FilletTool();
        var controller = new ToolController(context, tool);

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
        Assert.Equal(0, tool.Radius);
    }

    [Fact]
    public void PolylineAdjacentSegments_WhenSecondClickIsOnSharedVertex_ShouldSelectAdjacentSegment()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity filleted = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All));
        Assert.Contains(filleted.SegmentBulges, bulge => Math.Abs(bulge) > 1e-9);
    }


    [Fact]
    public void SeparateSingleSegmentPolylines_ShouldCreateTrimmedPolylinesAndFilletArc()
    {
        CadDocument document = new();
        var horizontal = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        var vertical = new PolylineEntity(new[]
        {
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        Assert.Equal(2, document.Entities.All.OfType<PolylineEntity>().Count());
        ArcEntity arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(2.0, arc.Radius, 12);
        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Count == 2 &&
            PointsNear(polyline.Vertices[0], new Point2D(0, 0)) &&
            PointsNear(polyline.Vertices[1], new Point2D(8, 0)));
        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Count == 2 &&
            PointsNear(polyline.Vertices[0], new Point2D(10, 2)) &&
            PointsNear(polyline.Vertices[1], new Point2D(10, 10)));
    }

    [Fact]
    public void SeparateMultiSegmentTerminalPolylines_ShouldTrimTerminalVerticesAndCreateFilletArc()
    {
        CadDocument document = new();
        var horizontal = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(20, 0)
        });
        var vertical = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(0, 10),
            new Point2D(0, 20)
        });
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        ArcEntity arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(2.0, arc.Radius, 12);
        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Count == 3 &&
            PointsNear(polyline.Vertices[0], new Point2D(2, 0)) &&
            PointsNear(polyline.Vertices[1], new Point2D(10, 0)) &&
            PointsNear(polyline.Vertices[2], new Point2D(20, 0)));
        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Count == 3 &&
            PointsNear(polyline.Vertices[0], new Point2D(0, 2)) &&
            PointsNear(polyline.Vertices[1], new Point2D(0, 10)) &&
            PointsNear(polyline.Vertices[2], new Point2D(0, 20)));
    }

    [Fact]
    public void SeparateMultiSegmentPolylines_WhenTrimWouldMoveInternalVertex_ShouldReturnClearError()
    {
        CadDocument document = new();
        var first = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(12, 0)
        });
        var second = new PolylineEntity(new[]
        {
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Fillet between separate polylines can only trim the terminal endpoint of a multi-segment polyline.", result.Message);
        Assert.Equal(2, document.Entities.All.Count());
    }

    private static bool PointsNear(Point2D actual, Point2D expected)
    {
        return Math.Abs(actual.X - expected.X) < 1e-6 &&
               Math.Abs(actual.Y - expected.Y) < 1e-6;
    }

    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 5);
    }
}
