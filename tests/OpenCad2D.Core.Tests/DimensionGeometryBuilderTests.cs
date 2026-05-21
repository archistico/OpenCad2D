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
        Assert.Equal(6, model.Arrows.Count);
        Assert.Equal("100.00", model.Text.Text);
        Assert.Equal(new Point2D(50, 6), model.Text.Position);
        Assert.Equal(0, model.Text.RotationDegrees);
        Assert.Contains(model.Lines, line =>
            line.Start == new Point2D(0, 20) &&
            line.End == new Point2D(100, 20));
    }

    [Fact]
    public void Build_WithVerticalDimension_ShouldUseLeftReadableTextAndMeasureDeltaY()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(20, 25),
            DimensionOrientation.Vertical);

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal("50.00", model.Text.Text);
        Assert.Equal(new Point2D(6, 25), model.Text.Position);
        Assert.Equal(270, model.Text.RotationDegrees);
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
    public void Build_WithVerticalDimensionLeftOfMeasuredPoints_ShouldKeepTextLeftOfDimensionLine()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(-20, 25),
            DimensionOrientation.Vertical);

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(new Point2D(-34, 25), model.Text.Position);
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
        Assert.Equal(6, model.Arrows.Count);
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
    public void Build_WithShortLinearDimensionAndAutoFit_ShouldPlaceTextOutsideMeasuredSpan()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(8, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithTextFitMode(DimensionTextFitMode.OutsideWhenNeeded);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.True(model.Text.Position.X > 8);
    }

    [Fact]
    public void Build_WithShortLinearDimensionAndInsideFit_ShouldKeepTextAtMidpoint()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(8, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithTextFitMode(DimensionTextFitMode.Inside);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Equal(4, model.Text.Position.X);
    }

    [Fact]
    public void Build_WithLongLinearDimensionAndAutoFit_ShouldKeepTextInsideMeasuredSpan()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithTextFitMode(DimensionTextFitMode.OutsideWhenNeeded);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Equal(50, model.Text.Position.X);
    }


    [Fact]
    public void Build_WithShortLinearDimensionAndAutoTerminatorFit_ShouldPlaceTerminatorsOutside()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(8, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithTerminatorFitMode(DimensionTerminatorFitMode.OutsideWhenNeeded);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Contains(model.Arrows, arrow => arrow.End.X < 0);
        Assert.Contains(model.Arrows, arrow => arrow.End.X > 8);
    }

    [Fact]
    public void Build_WithShortLinearDimensionAndInsideTerminatorFit_ShouldKeepTerminatorsInside()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(8, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithTerminatorFitMode(DimensionTerminatorFitMode.Inside);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.DoesNotContain(model.Arrows, arrow => arrow.End.X < 0);
        Assert.DoesNotContain(model.Arrows, arrow => arrow.End.X > 8);
    }

    [Fact]
    public void Build_WithLongLinearDimensionAndAutoTerminatorFit_ShouldKeepTerminatorsInside()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithTerminatorFitMode(DimensionTerminatorFitMode.OutsideWhenNeeded);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.DoesNotContain(model.Arrows, arrow => arrow.End.X < 0);
        Assert.DoesNotContain(model.Arrows, arrow => arrow.End.X > 100);
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
        Assert.Equal(3, model.Arrows.Count);
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
        Assert.Equal(6, model.Arrows.Count);
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

    [Fact]
    public void FormatMeasurement_WithPrefixAndSuffix_ShouldApplyStyleTextParts()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithText(
            TextFormatId.Annotation,
            decimalPlaces: 1,
            decimalSeparator: ".",
            suffix: " m",
            prefix: "≈ ");

        string text = _builder.FormatMeasurement(
            dimension,
            style);

        Assert.Equal("≈ 100.0 m", text);
    }


    [Fact]
    public void Build_WithOpenArrowSymbol_ShouldCreateTwoSegmentsPerTerminator()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(50, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithSymbols(
            DimensionArrowSymbol.OpenArrow,
            _style.ArrowSize);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Equal(4, model.Arrows.Count);
    }

    [Fact]
    public void Build_WithNoArrowSymbol_ShouldSuppressTerminators()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(50, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithSymbols(
            DimensionArrowSymbol.None,
            _style.ArrowSize);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Empty(model.Arrows);
    }


    [Theory]
    [InlineData(DimensionArrowSymbol.ClosedArrow, 6)]
    [InlineData(DimensionArrowSymbol.OpenArrow, 4)]
    [InlineData(DimensionArrowSymbol.ClosedBlankTriangle, 6)]
    [InlineData(DimensionArrowSymbol.ClosedFilledTriangle, 10)]
    [InlineData(DimensionArrowSymbol.FilledTriangleOutside, 10)]
    [InlineData(DimensionArrowSymbol.ArchitecturalTick, 2)]
    [InlineData(DimensionArrowSymbol.ObliqueSlash, 2)]
    [InlineData(DimensionArrowSymbol.Dot, 4)]
    [InlineData(DimensionArrowSymbol.None, 0)]
    public void Build_WithClassicArrowSymbols_ShouldCreateExpectedTerminatorSegments(
        DimensionArrowSymbol symbol,
        int expectedArrowSegments)
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(50, 20),
            DimensionOrientation.Horizontal);
        DimensionStyle style = _style.WithSymbols(
            symbol,
            _style.ArrowSize);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Equal(expectedArrowSegments, model.Arrows.Count);
    }

    [Fact]
    public void Build_WithUpsideDownAlignedDimension_ShouldUseReadableTextRotation()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(30, 40),
            new Point2D(0, 0),
            new Point2D(8, -6));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(53.13010235415598, model.Text.RotationDegrees, 10);
    }


    [Fact]
    public void Build_WithDownwardVerticalAlignedDimension_ShouldUseLeftReadableText()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 50),
            new Point2D(0, 0),
            new Point2D(12, 25));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        Assert.Equal(270, model.Text.RotationDegrees);
    }

    [Fact]
    public void Build_WithHorizontalTextRotationMode_ShouldKeepVerticalDimensionTextHorizontal()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(20, 25),
            DimensionOrientation.Vertical);
        DimensionStyle style = _style.WithOrientation(DimensionTextRotationMode.Horizontal);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Equal(0, model.Text.RotationDegrees);
    }

    [Fact]
    public void Build_WithAlignedTextRotationMode_ShouldKeepDownwardVerticalGeometricRotation()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 50),
            new Point2D(0, 0),
            new Point2D(12, 25));
        DimensionStyle style = _style.WithOrientation(DimensionTextRotationMode.AlignedWithDimensionLine);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Equal(270, model.Text.RotationDegrees);
    }

    [Fact]
    public void Build_WithAlignedTextRotationMode_ShouldKeepGeometricRotation()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(30, 40),
            new Point2D(0, 0),
            new Point2D(8, -6));
        DimensionStyle style = _style.WithOrientation(DimensionTextRotationMode.AlignedWithDimensionLine);

        DimensionRenderModel model = _builder.Build(dimension, style);

        Assert.Equal(233.13010235415598, model.Text.RotationDegrees, 10);
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
        Assert.Equal(6, model.Arrows.Count);
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
        Assert.Equal(45, model.Text.RotationDegrees, precision: 10);
    }
}
