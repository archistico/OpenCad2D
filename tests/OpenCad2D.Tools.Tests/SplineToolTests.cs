using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class SplineToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new SplineTool();

        Assert.Equal("Spline", tool.Name);
        Assert.Equal(SplineToolState.WaitingForFirstPoint, tool.State);
        Assert.Empty(tool.ControlPoints);
    }

    [Fact]
    public void PointerPresses_ThenEnter_ShouldCreateOpenSpline()
    {
        ToolContext context = CreateContext();
        var tool = new SplineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 10)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Confirm(""), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        BezierSplineEntity spline = Assert.Single(context.Document.Entities.All.OfType<BezierSplineEntity>());
        Assert.False(spline.IsClosed);
        Assert.Equal(3, spline.ControlPoints.Count);
    }

    [Fact]
    public void CloseOption_ShouldCreateClosedSpline()
    {
        ToolContext context = CreateContext();
        var tool = new SplineTool();

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("5,10", new Point2D(5, 10)), context);
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("C", "Close"), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        BezierSplineEntity spline = Assert.Single(context.Document.Entities.All.OfType<BezierSplineEntity>());
        Assert.True(spline.IsClosed);
    }

    [Fact]
    public void UndoOption_ShouldRemoveLastControlPoint()
    {
        ToolContext context = CreateContext();
        var tool = new SplineTool();

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("5,10", new Point2D(5, 10)), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("U", "Undo"), context);

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Single(tool.ControlPoints);
    }

    [Fact]
    public void PointerMove_ShouldReturnPreview()
    {
        ToolContext context = CreateContext();
        var tool = new SplineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 0)));

        BezierSplineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(2, preview.ControlPoints.Count);
    }

    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
