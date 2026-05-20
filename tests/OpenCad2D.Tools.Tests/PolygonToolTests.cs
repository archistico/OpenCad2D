using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class PolygonToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForSides()
    {
        var tool = new PolygonTool();

        Assert.Equal("Polygon", tool.Name);
        Assert.Equal(PolygonToolState.WaitingForSides, tool.State);
        Assert.Equal(PolygonTool.DefaultSideCount, tool.SideCount);
        Assert.Null(tool.Center);
        Assert.Null(tool.CurrentVertex);
    }

    [Fact]
    public void ConfirmAtSidePrompt_ShouldUseDefaultSideCount()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(PolygonToolState.WaitingForCenter, tool.State);
        Assert.Equal(PolygonTool.DefaultSideCount, tool.SideCount);
    }

    [Fact]
    public void SideCount_ShouldRejectValuesBelowMinimum()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("2", 2),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(PolygonToolState.WaitingForSides, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void SideCount_ShouldRejectNonIntegerValues()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("5.5", 5.5),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(PolygonToolState.WaitingForSides, tool.State);
    }

    [Fact]
    public void CommandInput_ShouldCreateClosedPolylinePolygon()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        ToolResult sideResult = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("5", 5),
            context);
        ToolResult centerResult = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", Point2D.Origin),
            context);
        ToolResult vertexResult = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)),
            context);

        Assert.Equal(ToolResultKind.Started, sideResult.Kind);
        Assert.Equal(ToolResultKind.Started, centerResult.Kind);
        Assert.Equal(ToolResultKind.Completed, vertexResult.Kind);
        Assert.Equal(PolygonToolState.WaitingForSides, tool.State);

        PolylineEntity polygon = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(polygon.IsClosed);
        Assert.Equal(5, polygon.Vertices.Count);
        AssertPointNear(new Point2D(10, 0), polygon.Vertices[0]);
    }

    [Fact]
    public void PointerInput_ShouldCreateClosedPolylinePolygon()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("4", 4),
            context);

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        PolylineEntity polygon = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(polygon.IsClosed);
        Assert.Equal(4, polygon.Vertices.Count);
        AssertPointNear(new Point2D(10, 0), polygon.Vertices[0]);
        AssertPointNear(new Point2D(0, 10), polygon.Vertices[1]);
        AssertPointNear(new Point2D(-10, 0), polygon.Vertices[2]);
        AssertPointNear(new Point2D(0, -10), polygon.Vertices[3]);
    }

    [Fact]
    public void PointerMove_AfterCenter_ShouldExposePreview()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3", 3),
            context);
        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.True(preview.IsClosed);
        Assert.Equal(3, preview.Vertices.Count);
    }

    [Fact]
    public void VertexEqualToCenter_ShouldNotCreatePolygonAndShouldKeepWaitingForVertex()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("6", 6),
            context);
        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", Point2D.Origin),
            context);

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", Point2D.Origin),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(PolygonToolState.WaitingForVertex, tool.State);
        Assert.Empty(context.Document.Entities.All);
    }

    [Fact]
    public void CreatedPolygon_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("4", 4),
            context);
        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", Point2D.Origin),
            context);
        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)),
            context);

        Assert.Single(context.Document.Entities.All);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Empty(context.Document.Entities.All);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void GetPromptState_ShouldExposePolygonCommandSteps()
    {
        var context = CreateContext();
        var tool = new PolygonTool();

        CommandPromptState sidesPrompt = tool.GetPromptState(context);

        Assert.Equal("POLYGON", sidesPrompt.CommandName);
        Assert.Equal(CommandInputKind.Number, sidesPrompt.ExpectedInput);
        Assert.True(sidesPrompt.AcceptsEmptyEnter);

        tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("6", 6),
            context);

        CommandPromptState centerPrompt = tool.GetPromptState(context);

        Assert.Equal(CommandInputKind.Point, centerPrompt.ExpectedInput);

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", Point2D.Origin),
            context);

        CommandPromptState vertexPrompt = tool.GetPromptState(context);

        Assert.Equal(CommandInputKind.PointOrDistance, vertexPrompt.ExpectedInput);
    }


    [Fact]
    public void ToolControllerConfirmActiveToolCommand_AtSidesPrompt_ShouldUseDefaultSideCount()
    {
        var context = CreateContext();
        var tool = new PolygonTool();
        var controller = new ToolController(context, tool);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(PolygonToolState.WaitingForCenter, tool.State);
        Assert.Equal(PolygonTool.DefaultSideCount, tool.SideCount);
    }
    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }

    private static void AssertPointNear(Point2D expected, Point2D actual)
    {
        Assert.True(
            expected.DistanceTo(actual) < 1e-6,
            $"Expected {expected}, actual {actual}.");
    }
}
