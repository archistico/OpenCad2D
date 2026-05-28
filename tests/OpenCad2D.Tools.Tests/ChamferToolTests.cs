using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class ChamferToolTests
{
    [Fact]
    public void DistanceOption_ShouldPromptForDistanceAndStoreValue()
    {
        ToolContext context = CreateContext();
        var tool = new ChamferTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("D", "Distance"),
            context);
        ToolResult distanceResult = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("2", 2),
            context);

        Assert.Equal(ToolResultKind.Started, optionResult.Kind);
        Assert.Equal(ToolResultKind.Started, distanceResult.Kind);
        Assert.Equal(2, tool.Distance);
        Assert.Equal(ChamferToolState.WaitingForFirstEntityOrDistance, tool.State);
    }

    [Fact]
    public void LineLine_ShouldCreateEqualDistanceChamferAndTrimLines()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());

        LineEntity chamfer = document.Entities.All
            .OfType<LineEntity>()
            .Single(line => PointsMatch(line.Start, new Point2D(8, 0)) &&
                            PointsMatch(line.End, new Point2D(10, 2)));

        AssertPointNear(new Point2D(8, 0), chamfer.Start);
        AssertPointNear(new Point2D(10, 2), chamfer.End);
    }

    [Fact]
    public void OnPointerMoved_AfterFirstLine_ShouldExposeChamferPreviewWithoutChangingDocument()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(3, tool.GetPreviewEntities().Count);
        Assert.Equal(2, document.Entities.All.Count());
    }

    [Fact]
    public void LineLine_WithParallelLines_ShouldNotModifyDocument()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(0, 2), new Point2D(10, 2));
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document);
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("parallel", result.Message);
        Assert.Equal(2, document.Entities.All.Count());
    }


    [Fact]
    public void LineLine_WhenSecondClickHitsBothLines_ShouldIgnoreFirstLineAndSelectSecond()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        Assert.Contains(document.Entities.All.OfType<LineEntity>(), line =>
            PointsMatch(line.Start, new Point2D(8, 0)) &&
            PointsMatch(line.End, new Point2D(10, 2)));
    }

    [Fact]
    public void GetActiveSnapKind_WhenSelectingEntities_ShouldUseEntityOnlySnap()
    {
        ToolContext context = CreateContext();
        var tool = new ChamferTool();

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
    }


    [Fact]
    public void PolylineAdjacentSegments_WithDistance_ShouldCreateSingleChamferedPolyline()
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
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity chamfered = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All));
        Assert.Equal(4, chamfered.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), chamfered.Vertices[0]);
        Assert.Equal(8.0, chamfered.Vertices[1].X, 12);
        Assert.Equal(0.0, chamfered.Vertices[1].Y, 12);
        Assert.Equal(10.0, chamfered.Vertices[2].X, 12);
        Assert.Equal(2.0, chamfered.Vertices[2].Y, 12);
        Assert.Equal(new Point2D(10, 10), chamfered.Vertices[3]);
        Assert.All(chamfered.SegmentBulges, bulge => Assert.Equal(0.0, bulge, 12));
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
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All));
    }

    [Fact]
    public void SeparateSingleSegmentPolylines_WithDistance_ShouldCreateTrimmedPolylinesAndChamferLine()
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
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        Assert.Equal(2, document.Entities.All.OfType<PolylineEntity>().Count());

        LineEntity chamfer = Assert.Single(document.Entities.All.OfType<LineEntity>());
        AssertPointNear(new Point2D(8, 0), chamfer.Start);
        AssertPointNear(new Point2D(10, 2), chamfer.End);

        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            PointsMatch(polyline.Vertices[0], new Point2D(0, 0)) &&
            PointsMatch(polyline.Vertices[1], new Point2D(8, 0)));
        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            PointsMatch(polyline.Vertices[0], new Point2D(10, 2)) &&
            PointsMatch(polyline.Vertices[1], new Point2D(10, 10)));
    }

    [Fact]
    public void LineAndSingleSegmentPolyline_WithDistance_ShouldCreateTrimmedLinePolylineAndChamferLine()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new PolylineEntity(new[]
        {
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        Assert.Equal(2, document.Entities.All.OfType<LineEntity>().Count());
        Assert.Single(document.Entities.All.OfType<PolylineEntity>());

        LineEntity chamfer = document.Entities.All
            .OfType<LineEntity>()
            .Single(line => PointsMatch(line.Start, new Point2D(8, 0)) &&
                            PointsMatch(line.End, new Point2D(10, 2)));
        AssertPointNear(new Point2D(8, 0), chamfer.Start);
        AssertPointNear(new Point2D(10, 2), chamfer.End);
    }

    [Fact]
    public void SeparateMultiSegmentTerminalPolylines_WithDistance_ShouldTrimTerminalVerticesAndCreateChamferLine()
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
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        LineEntity chamfer = Assert.Single(document.Entities.All.OfType<LineEntity>());
        AssertPointNear(new Point2D(2, 0), chamfer.Start);
        AssertPointNear(new Point2D(0, 2), chamfer.End);
        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Count == 3 &&
            PointsMatch(polyline.Vertices[0], new Point2D(2, 0)) &&
            PointsMatch(polyline.Vertices[1], new Point2D(10, 0)) &&
            PointsMatch(polyline.Vertices[2], new Point2D(20, 0)));
        Assert.Contains(document.Entities.All.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Count == 3 &&
            PointsMatch(polyline.Vertices[0], new Point2D(0, 2)) &&
            PointsMatch(polyline.Vertices[1], new Point2D(0, 10)) &&
            PointsMatch(polyline.Vertices[2], new Point2D(0, 20)));
    }

    [Fact]
    public void SeparateMultiSegmentPolylines_WhenTrimWouldMoveInternalVertex_ShouldReturnClearError()
    {
        CadDocument document = new();
        var first = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(20, 0)
        });
        var second = new PolylineEntity(new[]
        {
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document);
        var tool = new ChamferTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Distance"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(
            "Chamfer between separate polylines can only trim the terminal endpoint of a multi-segment polyline.",
            result.Message);
        Assert.Equal(2, document.Entities.All.Count());
    }

    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 5);
    }

    private static bool PointsMatch(Point2D actual, Point2D expected)
    {
        return Math.Abs(actual.X - expected.X) < 1e-6 &&
               Math.Abs(actual.Y - expected.Y) < 1e-6;
    }

    private static void AssertPointNear(Point2D expected, Point2D actual, int precision = 6)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
    }
}
