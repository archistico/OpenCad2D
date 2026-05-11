using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Measurements;

namespace OpenCad2D.Tools.Tests;

public sealed class MeasureDistanceToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new MeasureDistanceTool();

        Assert.Equal("Measure Distance", tool.Name);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreFirstPoint()
    {
        var context = CreateContext();
        var tool = new MeasureDistanceTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
        Assert.Equal(new Point2D(1, 2), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterFirstPoint_ShouldUpdatePreviewAndMessage()
    {
        var context = CreateContext();
        var tool = new MeasureDistanceTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(3, 4)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal("Distance: 5 | ΔX: 3 | ΔY: 4 | Angle: 53.13°", result.Message);
        Assert.Equal(new Point2D(3, 4), tool.CurrentPoint);

        LineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(Point2D.Origin, preview.Start);
        Assert.Equal(new Point2D(3, 4), preview.End);
    }

    [Fact]
    public void SecondPointerPress_ShouldReportDistanceAndNotCreateEntity()
    {
        var context = CreateContext();
        var tool = new MeasureDistanceTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 4)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Distance: 5 | ΔX: 3 | ΔY: 4 | Angle: 53.13°", result.Message);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.False(context.CommandHistory.CanUndo);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void SecondPointerPress_WithSamePoint_ShouldNotComplete()
    {
        var context = CreateContext();
        var tool = new MeasureDistanceTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void SecondPointerPress_WithEndpointSnapAndPolarTracking_ShouldApplySnapThenPolar()
    {
        var document = new CadDocument();

        var snapSource = new LineEntity(
            new Point2D(10, 10),
            new Point2D(20, 20));

        document.AddEntity(snapSource);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5,
            angleConstraintSettings: AngleConstraintSettings.FromStep(90));

        var tool = new MeasureDistanceTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(11, 9)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Distance: 14.142 | ΔX: 0 | ΔY: 14.142 | Angle: 90°", result.Message);
        Assert.Equal(1, context.Document.Entities.Count);
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
