using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Dimensions.Rendering;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class DimensionGeometryBuilderTests
{
    private readonly DimensionGeometryBuilder _builder = new();
    private readonly DimensionStyle _style = DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard);

    [Fact]
    public void Build_WithHorizontalDimension_ShouldCreateDimensionLineExtensionLinesArrowsAndText()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(50, 20),
            DimensionOrientation.Horizontal);

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(3, model.Lines.Count);
        Assert.Equal(4, model.Arrows.Count);
        Assert.Equal("100.00", model.Text.Text);
        Assert.Equal(new Point2D(50, 6), model.Text.Position);
        Assert.Equal(0, model.Text.RotationDegrees);
        Assert.Contains(model.Lines, line =>
            line.Start == new Point2D(0, 20) &&
            line.End == new Point2D(100, 20));
    }

    [Fact]
    public void Build_WithVerticalDimension_ShouldRotateTextAndMeasureDeltaY()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(20, 25),
            DimensionOrientation.Vertical);

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal("50.00", model.Text.Text);
        Assert.Equal(new Point2D(34, 25), model.Text.Position);
        Assert.Equal(90, model.Text.RotationDegrees);
        Assert.Contains(model.Lines, line =>
            line.Start == new Point2D(20, 0) &&
            line.End == new Point2D(20, 50));
    }


    [Fact]
    public void Build_WithHorizontalDimensionBelowMeasuredPoints_ShouldUseOppositeTextOffsetDirection()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(50, -20),
            DimensionOrientation.Horizontal);

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(new Point2D(50, -34), model.Text.Position);
    }

    [Fact]
    public void Build_WithVerticalDimensionLeftOfMeasuredPoints_ShouldKeepTextRightOfDimensionLine()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(-20, 25),
            DimensionOrientation.Vertical);

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(new Point2D(-6, 25), model.Text.Position);
    }

    [Fact]
    public void Build_WithAlignedDimension_ShouldUseMeasuredDirection()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(30, 40),
            new Point2D(-8, 6));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal("50.00", model.Text.Text);
        Assert.Equal(53.13010235415598, model.Text.RotationDegrees, 10);
        AssertPointNear(new Point2D(18.2, 17.6), model.Text.Position);
        Assert.Equal(3, model.Lines.Count);
        Assert.Equal(4, model.Arrows.Count);
    }


    [Fact]
    public void Build_WithAlignedDimensionOnOppositeSide_ShouldUseOppositeTextOffsetDirection()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(30, 40),
            new Point2D(8, -6));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        AssertPointNear(new Point2D(11.8, 22.4), model.Text.Position);
    }

    [Fact]
    public void Build_WithRadiusDimension_ShouldCreateLeaderArrowAndPrefixedText()
    {
        var dimension = new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(2, model.Lines.Count);
        Assert.Equal(2, model.Arrows.Count);
        Assert.Equal("R 10.00", model.Text.Text);
        Assert.Equal(new Point2D(14, 2), model.Text.Position);
        Assert.Equal(0, model.Text.RotationDegrees);
    }

    [Fact]
    public void Build_WithDiameterDimension_ShouldCreateDiameterLineArrowsAndPrefixedText()
    {
        var dimension = new DiameterDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(2, model.Lines.Count);
        Assert.Equal(4, model.Arrows.Count);
        Assert.Equal("Ø 20.00", model.Text.Text);
        Assert.Contains(model.Lines, line =>
            line.Start == new Point2D(-10, 0) &&
            line.End == new Point2D(10, 0));
    }

    [Fact]
    public void FormatMeasurement_WithOverride_ShouldUseOverride()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal,
            textOverride: " approx. ");

        string text = _builder.FormatMeasurement(
            dimension,
            _style);

        Assert.Equal("approx.", text);
    }
    private static void AssertPointNear(
        Point2D expected,
        Point2D actual,
        int precision = 10)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
    }
}

public sealed class AngularDimensionGeometryBuilderTests
{
    [Fact]
    public void Build_WithAngularDimension_ShouldCreateArcTextAndArrows()
    {
        var dimension = new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(10, 10),
            isCounterClockwise: true);
        var builder = new DimensionGeometryBuilder();

        DimensionRenderModel model = builder.Build(
            dimension,
            DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard));

        Assert.Equal(2, model.Lines.Count);
        Assert.Single(model.Arcs);
        Assert.Equal(4, model.Arrows.Count);
        Assert.Equal("90.00°", model.Text.Text);
        Assert.Equal(45, model.Text.RotationDegrees, precision: 10);
    }

    [Fact]
    public void Build_WithReflexAngularDimension_ShouldCreateLargeSweepText()
    {
        var dimension = new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(10, -10),
            isCounterClockwise: false);
        var builder = new DimensionGeometryBuilder();

        DimensionRenderModel model = builder.Build(
            dimension,
            DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard));

        DimensionArcPrimitive arc = Assert.Single(model.Arcs);
        Assert.False(arc.IsCounterClockwise);
        Assert.Equal("270.00°", model.Text.Text);
        Assert.Equal(225, model.Text.RotationDegrees, precision: 10);
    }
}
