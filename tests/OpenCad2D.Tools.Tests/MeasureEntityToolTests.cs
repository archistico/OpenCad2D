using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Measurements;

namespace OpenCad2D.Tools.Tests;

public sealed class MeasureEntityToolTests
{
    [Fact]
    public void Constructor_ShouldExposeNameAndEntitySnapMode()
    {
        var tool = new MeasureEntityTool();
        ToolContext context = CreateContext();

        Assert.Equal("Measure Entity", tool.Name);
        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
        Assert.Null(tool.LastMeasuredEntityId);
    }

    [Fact]
    public void ClickLine_ShouldReportLineMeasurementAndNotChangeDocument()
    {
        var document = new CadDocument();
        var line = new LineEntity(
            Point2D.Origin,
            new Point2D(3, 4));
        document.AddEntity(line);

        ToolContext context = CreateContext(document);
        var tool = new MeasureEntityTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 1)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Line | Length: 5 | Angle: 53.13°", result.Message);
        Assert.Equal(line.Id, tool.LastMeasuredEntityId);
        Assert.Equal(1, context.Document.Entities.Count);
        Assert.False(context.CommandHistory.CanUndo);
        Assert.Equal(0, context.SelectionSet.Count);
    }

    [Fact]
    public void ClickCircle_ShouldReportCircleMeasurement()
    {
        var document = new CadDocument();
        var circle = new CircleEntity(
            Point2D.Origin,
            10);
        document.AddEntity(circle);

        ToolContext context = CreateContext(document);
        var tool = new MeasureEntityTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Circle | Radius: 10 | Diameter: 20 | Circumference: 62.832 | Area: 314.159", result.Message);
        Assert.Equal(circle.Id, tool.LastMeasuredEntityId);
    }

    [Fact]
    public void ClickArc_ShouldReportArcMeasurement()
    {
        var document = new CadDocument();
        var arc = new ArcEntity(
            Point2D.Origin,
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));
        document.AddEntity(arc);

        ToolContext context = CreateContext(document);
        var tool = new MeasureEntityTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(7, 7)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Arc | Length: 15.708 | Radius: 10 | Diameter: 20 | Sweep: 90°", result.Message);
        Assert.Equal(arc.Id, tool.LastMeasuredEntityId);
    }

    [Fact]
    public void ClickClosedPolyline_ShouldReportPerimeterAreaAndVertices()
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
        var tool = new MeasureEntityTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Polyline | Length: 30 | Area: 50 | Vertices: 4 | Closed: Yes", result.Message);
        Assert.Equal(polyline.Id, tool.LastMeasuredEntityId);
    }

    [Fact]
    public void ClickEmptySpace_ShouldNotMeasureAnything()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureEntityTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("No measurable entity found.", result.Message);
        Assert.Null(tool.LastMeasuredEntityId);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ControlClick_ShouldCycleOverlappingEntities()
    {
        var document = new CadDocument();
        var first = new LineEntity(
            Point2D.Origin,
            new Point2D(10, 0),
            drawOrder: 0);
        var second = new LineEntity(
            Point2D.Origin,
            new Point2D(10, 0),
            drawOrder: 1);

        document.AddEntity(first);
        document.AddEntity(second);

        ToolContext context = CreateContext(document);
        var tool = new MeasureEntityTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0), PointerModifiers.Control));

        ToolResult secondResult = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0), PointerModifiers.Control));

        Assert.Equal(ToolResultKind.Completed, secondResult.Kind);
        Assert.Equal(first.Id, tool.LastMeasuredEntityId);
    }

    [Fact]
    public void Cancel_ShouldClearLastMeasuredEntity()
    {
        var document = new CadDocument();
        var line = new LineEntity(
            Point2D.Origin,
            new Point2D(10, 0));
        document.AddEntity(line);

        ToolContext context = CreateContext(document);
        var tool = new MeasureEntityTool();

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
