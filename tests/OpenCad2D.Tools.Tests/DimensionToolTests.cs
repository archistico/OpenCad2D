using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Dimensions;

namespace OpenCad2D.Tools.Tests;

public sealed class DimensionToolTests
{
    [Fact]
    public void HorizontalDimensionTool_ShouldCreateHorizontalDimensionAfterThreeClicks()
    {
        ToolContext context = CreateContext();
        var tool = new HorizontalDimensionTool();

        Assert.Equal(ToolResultKind.Started, tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0))).Kind);
        Assert.Equal(ToolResultKind.Started, tool.OnPointerPressed(context, new PointerInfo(new Point2D(100, 0))).Kind);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(50, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var dimension = Assert.Single(context.Document.Entities.All.OfType<LinearDimensionEntity>());
        Assert.Equal(DimensionOrientation.Horizontal, dimension.Orientation);
        Assert.Equal(100, dimension.MeasurementValue);
        Assert.Equal(new Point2D(50, 20), dimension.DimensionLinePoint);
    }

    [Fact]
    public void VerticalDimensionTool_ShouldCreateVerticalDimensionAfterThreeClicks()
    {
        ToolContext context = CreateContext();
        var tool = new VerticalDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 40)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(15, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var dimension = Assert.Single(context.Document.Entities.All.OfType<LinearDimensionEntity>());
        Assert.Equal(DimensionOrientation.Vertical, dimension.Orientation);
        Assert.Equal(40, dimension.MeasurementValue);
    }

    [Fact]
    public void AlignedDimensionTool_ShouldCreateAlignedDimensionAfterThreeClicks()
    {
        ToolContext context = CreateContext();
        var tool = new AlignedDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(30, 40)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(-8, 6)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var dimension = Assert.Single(context.Document.Entities.All.OfType<AlignedDimensionEntity>());
        Assert.Equal(50, dimension.MeasurementValue);
        Assert.Equal(new Point2D(-8, 6), dimension.DimensionLinePoint);
    }

    [Fact]
    public void HorizontalDimensionTool_WithInvalidMeasuredPoints_ShouldNotCreateDimension()
    {
        ToolContext context = CreateContext();
        var tool = new HorizontalDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(context.Document.Entities.All);
    }

    [Fact]
    public void CreatedDimension_ShouldBeUndoable()
    {
        ToolContext context = CreateContext();
        var tool = new HorizontalDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(100, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(50, 20)));

        Assert.Single(context.Document.Entities.All);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Empty(context.Document.Entities.All);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void PointerMove_AfterSecondPoint_ShouldExposeDimensionPreview()
    {
        ToolContext context = CreateContext();
        var tool = new HorizontalDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(100, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(50, 20)));

        var preview = Assert.Single(tool.GetPreviewEntities().OfType<LinearDimensionEntity>());

        Assert.Equal(DimensionOrientation.Horizontal, preview.Orientation);
        Assert.Equal(new Point2D(50, 20), preview.DimensionLinePoint);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
