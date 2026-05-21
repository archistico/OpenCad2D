using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
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

    [Fact]
    public void RadiusDimensionTool_ShouldCreateRadiusDimensionAfterThreeClicks()
    {
        ToolContext context = CreateContext();
        var tool = new RadiusDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(14, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var dimension = Assert.Single(context.Document.Entities.All.OfType<RadiusDimensionEntity>());
        Assert.Equal(10, dimension.MeasurementValue);
        Assert.Equal(new Point2D(14, 2), dimension.TextPoint);
    }

    [Fact]
    public void DiameterDimensionTool_ShouldCreateDiameterDimensionAfterThreeClicks()
    {
        ToolContext context = CreateContext();
        var tool = new DiameterDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(14, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var dimension = Assert.Single(context.Document.Entities.All.OfType<DiameterDimensionEntity>());
        Assert.Equal(20, dimension.MeasurementValue);
        Assert.Equal(new Point2D(-10, 0), dimension.OppositePoint);
    }

    [Fact]
    public void RadiusDimensionTool_WithInvalidCirclePoint_ShouldNotCreateDimension()
    {
        ToolContext context = CreateContext();
        var tool = new RadiusDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(context.Document.Entities.All);
    }

    [Fact]
    public void RadiusDimensionTool_AfterSecondPoint_ShouldExposeDimensionPreview()
    {
        ToolContext context = CreateContext();
        var tool = new RadiusDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(14, 2)));

        var preview = Assert.Single(tool.GetPreviewEntities().OfType<RadiusDimensionEntity>());

        Assert.Equal(10, preview.MeasurementValue);
        Assert.Equal(new Point2D(14, 2), preview.TextPoint);
    }


    [Fact]
    public void AngularDimensionTool_ShouldCreateMinorAngularDimensionAfterFourClicks()
    {
        ToolContext context = CreateContext();
        var tool = new AngularDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, 8)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var dimension = Assert.Single(context.Document.Entities.All.OfType<AngularDimensionEntity>());
        Assert.True(dimension.IsCounterClockwise);
        Assert.Equal(90, dimension.MeasurementValue, precision: 10);
    }

    [Fact]
    public void AngularDimensionTool_ShouldCreateReflexAngularDimensionWhenArcPointIsOutsideMinorSector()
    {
        ToolContext context = CreateContext();
        var tool = new AngularDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(8, -8)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var dimension = Assert.Single(context.Document.Entities.All.OfType<AngularDimensionEntity>());
        Assert.False(dimension.IsCounterClockwise);
        Assert.Equal(270, dimension.MeasurementValue, precision: 10);
    }

    [Fact]
    public void AngularDimensionTool_AfterThirdPoint_ShouldExposeDimensionPreview()
    {
        ToolContext context = CreateContext();
        var tool = new AngularDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(8, -8)));

        var preview = Assert.Single(tool.GetPreviewEntities().OfType<AngularDimensionEntity>());

        Assert.False(preview.IsCounterClockwise);
        Assert.Equal(270, preview.MeasurementValue, precision: 10);
    }

    [Fact]
    public void AngularDimensionTool_WithInvalidFirstRay_ShouldNotCreateDimension()
    {
        ToolContext context = CreateContext();
        var tool = new AngularDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(context.Document.Entities.All);
    }


    [Fact]
    public void HorizontalDimensionTool_ShouldAssignCurrentDimensionStyle()
    {
        var document = new CadDocument();
        var customStyleId = new DimensionStyleId("Architectural");
        document.ReplaceDimensionStyles(new DimensionStyleCollection(new[]
        {
            DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard),
            new DimensionStyle(
                customStyleId,
                "Architectural",
                TextFormatId.Annotation,
                arrowSize: 4,
                textOffset: 2,
                extensionLineOffset: 1.5,
                extensionLineOvershoot: 2,
                decimalPlaces: 2,
                suffix: " m")
        }));
        document.SetCurrentDimensionStyle(customStyleId);
        ToolContext context = CreateContext(document);
        var tool = new HorizontalDimensionTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(100, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(50, 20)));

        var dimension = Assert.Single(context.Document.Entities.All.OfType<LinearDimensionEntity>());
        Assert.Equal(customStyleId, dimension.DimensionStyleId);
    }
    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
