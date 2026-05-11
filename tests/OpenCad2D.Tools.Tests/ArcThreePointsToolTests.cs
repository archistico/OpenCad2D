using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.Tools.Tests;

public sealed class ArcThreePointsToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForStartPoint()
    {
        var tool = new ArcThreePointsTool();

        Assert.Equal("Arc 3P", tool.Name);
        Assert.Equal(ArcThreePointsToolState.WaitingForStartPoint, tool.State);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.PointOnArc);
        Assert.Null(tool.CurrentPoint);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreStartPoint()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ArcThreePointsToolState.WaitingForPointOnArc, tool.State);
        Assert.Equal(new Point2D(10, 20), tool.StartPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void SecondPointerPress_ShouldStorePointOnArc()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ArcThreePointsToolState.WaitingForEndPoint, tool.State);
        Assert.Equal(new Point2D(0, 10), tool.PointOnArc);
        Assert.Equal(new Point2D(0, 10), tool.CurrentPoint);
        Assert.Equal(new Point2D(0, 10), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterPointOnArc_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(-10, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(-10, 0), tool.CurrentPoint);

        ArcEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(0, 0), preview.Center);
        Assert.Equal(10, preview.Radius, precision: 10);
        Assert.True(preview.IsCounterClockwise);
    }

    [Fact]
    public void ThirdPointerPress_ShouldCreateArcEntity()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(-10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(ArcThreePointsToolState.WaitingForStartPoint, tool.State);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.PointOnArc);
        Assert.Null(tool.CurrentPoint);
        Assert.Null(context.CurrentBasePoint);

        var arc = Assert.Single(context.Document.Entities.All.OfType<ArcEntity>());

        Assert.Equal(new Point2D(0, 0), arc.Center);
        Assert.Equal(10, arc.Radius, precision: 10);
        Assert.Equal(0, arc.StartAngle.Degrees, precision: 10);
        Assert.Equal(180, arc.EndAngle.Degrees, precision: 10);
        Assert.True(arc.IsCounterClockwise);
    }

    [Fact]
    public void ThirdPointerPress_WithClockwiseMiddlePoint_ShouldCreateClockwiseArc()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, -10)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(-10, 0)));

        var arc = Assert.Single(context.Document.Entities.All.OfType<ArcEntity>());

        Assert.False(arc.IsCounterClockwise);
    }

    [Fact]
    public void PointOnArcPress_WithSameStartPoint_ShouldNotAdvance()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ArcThreePointsToolState.WaitingForPointOnArc, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void EndPointerPress_WithCollinearPoints_ShouldNotCreateArc()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(20, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ArcThreePointsToolState.WaitingForEndPoint, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void CreatedArc_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(-10, 0)));

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void CreatedArc_ShouldBeOnCurrentLayer()
    {
        CadDocument document = new();
        var layerId = new LayerId("Arcs");

        document.Layers.Add(
            new Layer(
                layerId,
                "Arcs",
                CadColor.FromRgb(255, 0, 0),
                LineWeight.FromMillimeters(0.25)));

        var context = new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            currentLayerId: layerId);

        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(-10, 0)));

        var arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());

        Assert.Equal(layerId, arc.LayerId);
    }

    [Fact]
    public void PointerMove_WithPolarTracking_ShouldConstrainPreviewPoint()
    {
        var context = CreateContext();
        context.AngleConstraintSettings = new AngleConstraintSettings(
            isEnabled: true,
            stepDegrees: 45);

        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(20, 3)));

        Assert.NotNull(tool.CurrentPoint);
        Assert.Equal(20.44030650891055, tool.CurrentPoint.Value.X, precision: 10);
        Assert.Equal(0, tool.CurrentPoint.Value.Y, precision: 10);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContext();
        var tool = new ArcThreePointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(ArcThreePointsToolState.WaitingForStartPoint, tool.State);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.PointOnArc);
        Assert.Null(tool.CurrentPoint);
        Assert.Null(context.CurrentBasePoint);
        Assert.False(tool.HasPreview);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance);
    }
}
