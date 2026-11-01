using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class ArcToolRegressionTests
{
    [Fact]
    public void TypedEndPointWithOrthoEnabled_ShouldStayExact()
    {
        ToolContext context = CreateContext(out CadDocument document);
        context.IsOrthoEnabled = true;
        var tool = new ArcTool();
        double radians = 30.0 * Math.PI / 180.0;

        Submit(tool, context, new Point2D(0, 0));
        Submit(tool, context, new Point2D(10, 0));
        ToolResult result = Submit(tool, context, new Point2D(10 * Math.Cos(radians), 10 * Math.Sin(radians)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        ArcEntity arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(30, arc.EndAngle.Degrees, precision: 9);
    }

    [Fact]
    public void TypedCenterNearSnapTarget_ShouldNotSnap()
    {
        ToolContext context = CreateContext(out CadDocument document, SnapKind.Endpoint, snapTolerance: 5);
        document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(100, 0)));
        var tool = new ArcTool();

        Submit(tool, context, new Point2D(1, 1));

        Assert.Equal(new Point2D(1, 1), tool.CenterPoint);
    }

    [Fact]
    public void TypedAngleInEndPhase_ShouldCreateArcEndingAtThatAngle()
    {
        ToolContext context = CreateContext(out CadDocument document);
        var tool = new ArcTool();

        Submit(tool, context, new Point2D(0, 0));
        Submit(tool, context, new Point2D(10, 0));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Angle("90", 90), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        ArcEntity arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(10, arc.Radius, precision: 9);
        Assert.Equal(0, arc.Geometry.EndPoint.X, precision: 9);
        Assert.Equal(10, arc.Geometry.EndPoint.Y, precision: 9);
        Assert.True(arc.IsCounterClockwise);
    }

    [Fact]
    public void DirectionOption_ShouldCreateClockwiseArcAndPreview()
    {
        ToolContext context = CreateContext(out CadDocument document);
        var tool = new ArcTool();

        Submit(tool, context, new Point2D(0, 0));
        Submit(tool, context, new Point2D(10, 0));

        CommandPromptState prompt = tool.GetPromptState(context);
        Assert.Contains(prompt.Options, option => option.Keyword == "Direction" && option.Shortcut == "D");
        Assert.Contains("counter-clockwise", prompt.Prompt);

        ToolResult toggled = tool.HandleCommandInput(CommandInputSubmission.Option("D", "Direction"), context);

        Assert.Equal(ToolResultKind.Started, toggled.Kind);
        Assert.False(tool.IsCounterClockwise);
        Assert.Equal(ArcToolState.WaitingForEndPoint, tool.State);
        Assert.DoesNotContain("counter-clockwise", tool.GetPromptState(context).Prompt);

        tool.OnPointerMoved(context, new PointerInfo(new Point2D(0, 10)));
        ArcEntity preview = Assert.IsType<ArcEntity>(tool.GetPreviewEntity());
        Assert.False(preview.IsCounterClockwise);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        ArcEntity arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.False(arc.IsCounterClockwise);
        Assert.Equal(0, arc.StartAngle.Degrees, precision: 9);
        Assert.Equal(90, arc.EndAngle.Degrees, precision: 9);
    }

    [Fact]
    public void DirectionOption_ShouldBeAvailableInEveryPhaseAndKeptForNextArc()
    {
        ToolContext context = CreateContext(out CadDocument document);
        var tool = new ArcTool();

        Assert.Contains(tool.GetPromptState(context).Options, option => option.Keyword == "Direction");
        tool.HandleCommandInput(CommandInputSubmission.Option("D", "Direction"), context);
        Assert.Equal(ArcToolState.WaitingForCenterPoint, tool.State);

        Submit(tool, context, new Point2D(0, 0));
        Assert.Contains(tool.GetPromptState(context).Options, option => option.Keyword == "Direction");
        Submit(tool, context, new Point2D(10, 0));
        Submit(tool, context, new Point2D(0, 10));

        Assert.False(tool.IsCounterClockwise);
        Assert.False(Assert.Single(document.Entities.All.OfType<ArcEntity>()).IsCounterClockwise);
    }

    [Fact]
    public void ConfirmAtCenterPrompt_ShouldFinishCommand()
    {
        ToolContext context = CreateContext(out _);
        var tool = new ArcTool();
        var controller = new ToolController(context, tool);

        Assert.True(tool.GetPromptState(context).AcceptsEmptyEnter);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<OpenCad2D.Tools.Selection.SelectionTool>(controller.ActiveTool);
    }

    [Fact]
    public void ArcThreePoints_ConfirmAtStartPrompt_ShouldFinishCommand()
    {
        ToolContext context = CreateContext(out _);
        var tool = new ArcThreePointsTool();
        var controller = new ToolController(context, tool);

        Assert.True(tool.GetPromptState(context).AcceptsEmptyEnter);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<OpenCad2D.Tools.Selection.SelectionTool>(controller.ActiveTool);
    }

    private static ToolResult Submit(ArcTool tool, ToolContext context, Point2D point)
    {
        return tool.HandleCommandInput(
            CommandInputSubmission.FromPoint($"{point.X},{point.Y}", point),
            context);
    }

    private static ToolContext CreateContext(
        out CadDocument document,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0)
    {
        document = new CadDocument();
        return new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance);
    }
}
