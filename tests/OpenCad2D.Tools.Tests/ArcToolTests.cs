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

public sealed class ArcToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForCenterPoint()
    {
        var tool = new ArcTool();

        Assert.Equal("Arc", tool.Name);
        Assert.Equal(ArcToolState.WaitingForCenterPoint, tool.State);
        Assert.Null(tool.CenterPoint);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreCenterPoint()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ArcToolState.WaitingForStartPoint, tool.State);
        Assert.Equal(new Point2D(10, 20), tool.CenterPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void SecondPointerPress_ShouldStoreStartPoint()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ArcToolState.WaitingForEndPoint, tool.State);
        Assert.Equal(new Point2D(10, 0), tool.StartPoint);
        Assert.Equal(new Point2D(10, 0), tool.CurrentPoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterStartPoint_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(0, 10), tool.CurrentPoint);

        ArcEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(0, 0), preview.Center);
        Assert.Equal(10, preview.Radius, precision: 10);
        Assert.Equal(0, preview.StartAngle.Degrees, precision: 10);
        Assert.Equal(90, preview.EndAngle.Degrees, precision: 10);
    }

    [Fact]
    public void ThirdPointerPress_ShouldCreateArcEntity()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(ArcToolState.WaitingForCenterPoint, tool.State);
        Assert.Null(tool.CenterPoint);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.Null(context.CurrentBasePoint);

        var arc = Assert.Single(context.Document.Entities.All.OfType<ArcEntity>());

        Assert.Equal(new Point2D(0, 0), arc.Center);
        Assert.Equal(10, arc.Radius, precision: 10);
        Assert.Equal(0, arc.StartAngle.Degrees, precision: 10);
        Assert.Equal(90, arc.EndAngle.Degrees, precision: 10);
        Assert.True(arc.IsCounterClockwise);
    }

    [Fact]
    public void StartPointerPress_WithSameCenterPoint_ShouldNotCreateArc()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ArcToolState.WaitingForStartPoint, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void EndPointerPress_WithCenterPoint_ShouldNotCreateArc()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ArcToolState.WaitingForEndPoint, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void CreatedArc_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

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

        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        var arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());

        Assert.Equal(layerId, arc.LayerId);
    }

    [Fact]
    public void PointerMove_WithPolarTracking_ShouldConstrainPreviewDirection()
    {
        var context = CreateContext();
        context.AngleConstraintSettings = new AngleConstraintSettings(
            isEnabled: true,
            stepDegrees: 45);

        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 3)));

        Assert.NotNull(tool.CurrentPoint);
        Assert.Equal(10.44030650891055, tool.CurrentPoint.Value.X, precision: 10);
        Assert.Equal(0, tool.CurrentPoint.Value.Y, precision: 10);

        ArcEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(0, preview.EndAngle.Degrees, precision: 10);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContext();
        var tool = new ArcTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(ArcToolState.WaitingForCenterPoint, tool.State);
        Assert.Null(tool.CenterPoint);
        Assert.Null(tool.StartPoint);
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
