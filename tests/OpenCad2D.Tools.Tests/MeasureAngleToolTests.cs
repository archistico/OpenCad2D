using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Measurements;

namespace OpenCad2D.Tools.Tests;

public sealed class MeasureAngleToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstRayPoint()
    {
        var tool = new MeasureAngleTool();

        Assert.Equal("Measure Angle", tool.Name);
        Assert.Equal(MeasureAngleToolState.WaitingForFirstRayPoint, tool.State);
        Assert.Null(tool.FirstRayPoint);
        Assert.Null(tool.Vertex);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreFirstRayPoint()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(MeasureAngleToolState.WaitingForVertex, tool.State);
        Assert.Equal(new Point2D(10, 0), tool.FirstRayPoint);
        Assert.Equal(new Point2D(10, 0), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void SecondPointerPress_ShouldStoreVertex()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(MeasureAngleToolState.WaitingForSecondRayPoint, tool.State);
        Assert.Equal(Point2D.Origin, tool.Vertex);
        Assert.Equal(Point2D.Origin, context.CurrentBasePoint);
    }

    [Fact]
    public void PointerMove_AfterVertex_ShouldUpdatePreviewAndMessage()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal("Angle: 90° | Supplementary: 90°", result.Message);
        Assert.Equal(new Point2D(0, 10), tool.CurrentPoint);

        IReadOnlyList<LineEntity> preview = tool.GetPreviewEntities();

        Assert.Equal(2, preview.Count);
        Assert.Equal(Point2D.Origin, preview[0].Start);
        Assert.Equal(new Point2D(10, 0), preview[0].End);
        Assert.Equal(Point2D.Origin, preview[1].Start);
        Assert.Equal(new Point2D(0, 10), preview[1].End);
    }

    [Fact]
    public void ThirdPointerPress_ShouldReportAngleAndNotCreateEntity()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Angle: 90° | Supplementary: 90°", result.Message);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.False(context.CommandHistory.CanUndo);
        Assert.Equal(MeasureAngleToolState.WaitingForFirstRayPoint, tool.State);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void SecondPointerPress_SameAsFirstPoint_ShouldNotAdvance()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(MeasureAngleToolState.WaitingForVertex, tool.State);
    }

    [Fact]
    public void ThirdPointerPress_SameAsVertex_ShouldNotComplete()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(MeasureAngleToolState.WaitingForSecondRayPoint, tool.State);
    }

    [Fact]
    public void ThirdPointerPress_WithEndpointSnapAndPolarTracking_ShouldApplySnapThenPolar()
    {
        var document = new CadDocument();
        var snapSource = new LineEntity(
            new Point2D(10, 10),
            new Point2D(20, 20));
        document.AddEntity(snapSource);

        ToolContext context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5,
            angleConstraintSettings: AngleConstraintSettings.FromStep(45));

        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(11, 9)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Angle: 45° | Supplementary: 135°", result.Message);
        Assert.Equal(1, context.Document.Entities.Count);
    }

    [Fact]
    public void Cancel_ShouldResetState()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(MeasureAngleToolState.WaitingForFirstRayPoint, tool.State);
        Assert.Null(tool.FirstRayPoint);
        Assert.Null(context.CurrentBasePoint);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0,
        AngleConstraintSettings? angleConstraintSettings = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance,
            angleConstraintSettings: angleConstraintSettings);
    }
}
