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

public sealed class CircleToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForCenterPoint()
    {
        var tool = new CircleTool();

        Assert.Equal("Circle", tool.Name);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreCenterPoint()
    {
        var context = CreateContext();
        var tool = new CircleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(10, 20), tool.FirstPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterCenterPoint_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new CircleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(13, 24)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(13, 24), tool.CurrentPoint);

        CircleEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(10, 20), preview.Center);
        Assert.Equal(5, preview.Radius);
    }

    [Fact]
    public void SecondPointerPress_ShouldCreateCircleEntity()
    {
        var context = CreateContext();
        var tool = new CircleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(13, 24)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.Null(context.CurrentBasePoint);

        var circle = Assert.Single(context.Document.Entities.All.OfType<CircleEntity>());

        Assert.Equal(new Point2D(10, 20), circle.Center);
        Assert.Equal(5, circle.Radius);
    }

    [Fact]
    public void SecondPointerPress_WithSamePoint_ShouldNotCreateCircle()
    {
        var context = CreateContext();
        var tool = new CircleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
    }

    [Fact]
    public void CreatedCircle_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new CircleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void SecondPointerPress_ShouldCreateCircleOnCurrentLayer()
    {
        CadDocument document = new();
        var layerId = new LayerId("Circles");

        document.Layers.Add(
            new Layer(
                layerId,
                "Circles",
                CadColor.FromRgb(255, 0, 0),
                LineWeight.FromMillimeters(0.25)));

        var context = new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            currentLayerId: layerId);

        var tool = new CircleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        var circle = Assert.Single(document.Entities.All.OfType<CircleEntity>());

        Assert.Equal(layerId, circle.LayerId);
    }

    [Fact]
    public void PointerMove_WithOrthoEnabled_ShouldConstrainPreviewPoint()
    {
        var context = CreateContext();
        context.IsOrthoEnabled = true;

        var tool = new CircleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 3)));

        Assert.Equal(new Point2D(10, 0), tool.CurrentPoint);

        CircleEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(10, preview.Radius);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
