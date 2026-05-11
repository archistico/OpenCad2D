using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Measurements;

namespace OpenCad2D.Tools.Tests;

public sealed class MeasureAreaToolTests
{
    [Fact]
    public void Constructor_ShouldExposeNameAndEntitySnapMode()
    {
        var tool = new MeasureAreaTool();
        ToolContext context = CreateContext();

        Assert.Equal("Measure Area", tool.Name);
        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
        Assert.Null(tool.LastMeasuredEntityId);
    }

    [Fact]
    public void ClickClosedPolyline_ShouldReportAreaPerimeterAndVertices()
    {
        var document = new CadDocument();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true);
        document.AddEntity(polyline);

        ToolContext context = CreateContext(document);
        var tool = new MeasureAreaTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Polyline | Length: 30 | Area: 50 | Vertices: 4 | Closed: Yes", result.Message);
        Assert.Equal(polyline.Id, tool.LastMeasuredEntityId);
        Assert.Equal(1, context.Document.Entities.Count);
        Assert.False(context.CommandHistory.CanUndo);
        Assert.Equal(0, context.SelectionSet.Count);
    }

    [Fact]
    public void ClickOpenPolyline_ShouldRejectAreaMeasurement()
    {
        var document = new CadDocument();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5)
            },
            isClosed: false);
        document.AddEntity(polyline);

        ToolContext context = CreateContext(document);
        var tool = new MeasureAreaTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Area can only be measured from a closed polyline.", result.Message);
        Assert.Equal(polyline.Id, tool.LastMeasuredEntityId);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ClickLine_ShouldRejectAreaMeasurement()
    {
        var document = new CadDocument();
        var line = new LineEntity(
            Point2D.Origin,
            new Point2D(10, 0));
        document.AddEntity(line);

        ToolContext context = CreateContext(document);
        var tool = new MeasureAreaTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Area can only be measured from a closed polyline.", result.Message);
        Assert.Equal(line.Id, tool.LastMeasuredEntityId);
        Assert.Equal(0, context.SelectionSet.Count);
    }

    [Fact]
    public void ClickEmptySpace_ShouldNotMeasureAnything()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAreaTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("No closed polyline found.", result.Message);
        Assert.Null(tool.LastMeasuredEntityId);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ControlClick_ShouldCycleOverlappingEntities()
    {
        var document = new CadDocument();
        var first = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true,
            drawOrder: 0);
        var second = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 2),
                new Point2D(0, 2)
            },
            isClosed: true,
            drawOrder: 1);

        document.AddEntity(first);
        document.AddEntity(second);

        ToolContext context = CreateContext(document);
        var tool = new MeasureAreaTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        EntityId? firstMeasuredId = tool.LastMeasuredEntityId;

        ToolResult secondResult = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0), PointerModifiers.Control));

        Assert.Equal(ToolResultKind.Completed, secondResult.Kind);
        Assert.NotEqual(firstMeasuredId, tool.LastMeasuredEntityId);
    }

    [Fact]
    public void Cancel_ShouldClearLastMeasuredEntity()
    {
        var document = new CadDocument();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true);
        document.AddEntity(polyline);

        ToolContext context = CreateContext(document);
        var tool = new MeasureAreaTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Null(tool.LastMeasuredEntityId);
    }

    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 1);
    }
}
