using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Dimensions.Rendering;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class DimensionEdgeCaseTests
{
    private readonly DimensionGeometryBuilder _builder = new();
    private readonly DimensionStyle _style = DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard);

    [Fact]
    public void LinearDimension_WithZeroHorizontalDistance_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new LinearDimensionEntity(
            new Point2D(10, 0),
            new Point2D(10, 25),
            new Point2D(10, 40),
            DimensionOrientation.Horizontal));
    }

    [Fact]
    public void LinearDimension_WithZeroVerticalDistance_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new LinearDimensionEntity(
            new Point2D(0, 10),
            new Point2D(25, 10),
            new Point2D(40, 10),
            DimensionOrientation.Vertical));
    }

    [Fact]
    public void AlignedDimension_WithCoincidentMeasuredPoints_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new AlignedDimensionEntity(
            new Point2D(5, 5),
            new Point2D(5, 5),
            new Point2D(10, 10)));
    }

    [Fact]
    public void RadiusDimension_WithZeroRadius_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0)));
    }

    [Fact]
    public void DiameterDimension_WithZeroRadius_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new DiameterDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0)));
    }

    [Fact]
    public void AngularDimension_WithFirstRayPointAtCenter_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(8, 8),
            isCounterClockwise: true));
    }

    [Fact]
    public void AngularDimension_WithSecondRayPointAtCenter_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 0),
            new Point2D(8, 8),
            isCounterClockwise: true));
    }

    [Fact]
    public void AngularDimension_WithArcPointAtCenter_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(0, 0),
            isCounterClockwise: true));
    }

    [Fact]
    public void AngularDimension_WithCoincidentRayDirections_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(25, 0),
            new Point2D(8, 8),
            isCounterClockwise: true));
    }

    [Fact]
    public void Build_WithNegativeCoordinates_ShouldCreateFiniteBounds()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(-100, -50),
            new Point2D(-40, -10),
            new Point2D(-80, 20));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        AssertFinite(model.Bounds);
        Assert.True(model.Bounds.MinX < 0);
        Assert.True(model.Bounds.MinY < 0);
    }

    [Fact]
    public void Build_WithVerySmallDimension_ShouldCreateFiniteBounds()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0.0001, 0.0001),
            new Point2D(0.0002, 0));

        DimensionRenderModel model = _builder.Build(dimension, _style);

        AssertFinite(model.Bounds);
        Assert.Equal("0.00", model.Text.Text);
    }

    [Fact]
    public void Build_WithVeryLargeDimension_ShouldCreateFiniteBounds()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(-1_000_000, 0),
            new Point2D(1_000_000, 0),
            new Point2D(0, 100_000),
            DimensionOrientation.Horizontal);

        DimensionRenderModel model = _builder.Build(dimension, _style);

        AssertFinite(model.Bounds);
        Assert.Equal("2000000.00", model.Text.Text);
    }

    [Fact]
    public void LinearDimension_RotatedByNinetyDegrees_ShouldBecomeVerticalDimension()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, 4),
            DimensionOrientation.Horizontal);

        var transformed = Assert.IsType<LinearDimensionEntity>(dimension.Transform(
            Matrix2D.Rotation(Math.PI / 2.0, new Point2D(0, 0))));

        Assert.Equal(DimensionOrientation.Vertical, transformed.Orientation);
        Assert.Equal(10, transformed.MeasurementValue, precision: 10);
    }

    [Fact]
    public void LinearDimension_RotatedByArbitraryAngle_ShouldBecomeAlignedDimension()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, 4),
            DimensionOrientation.Horizontal,
            textOverride: "A");

        var transformed = Assert.IsType<AlignedDimensionEntity>(dimension.Transform(
            Matrix2D.Rotation(Math.PI / 4.0, new Point2D(0, 0))));

        Assert.Equal(dimension.Id, transformed.Id);
        Assert.Equal("A", transformed.TextOverride);
        Assert.Equal(10, transformed.MeasurementValue, precision: 10);
    }

    [Fact]
    public void AngularDimension_MirroredAcrossYAxis_ShouldFlipSweepDirectionAndKeepMeasurement()
    {
        var dimension = new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, 8),
            isCounterClockwise: true);
        Line2D mirrorLine = Line2D.FromPoints(
            new Point2D(0, -10),
            new Point2D(0, 10));

        var transformed = Assert.IsType<AngularDimensionEntity>(dimension.Transform(
            Matrix2D.Mirror(mirrorLine)));

        Assert.False(transformed.IsCounterClockwise);
        Assert.Equal(90, transformed.MeasurementValue, precision: 10);
        Assert.Equal(new Point2D(-10, 0), transformed.FirstRayPoint);
        Assert.Equal(new Point2D(0, 10), transformed.SecondRayPoint);
    }

    [Fact]
    public void DimensionDistanceTo_ShouldReturnZeroForPointOnDimensionGeometry()
    {
        var horizontal = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(50, 20),
            DimensionOrientation.Horizontal);
        var aligned = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(30, 40),
            new Point2D(-8, 6));
        var radius = new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(15, 0));

        Assert.Equal(0, horizontal.DistanceTo(new Point2D(50, 20)), precision: 10);
        Assert.Equal(0, aligned.DistanceTo(aligned.GetClosestPoint(new Point2D(10, 10))), precision: 10);
        Assert.Equal(0, radius.DistanceTo(new Point2D(12, 0)), precision: 10);
    }

    private static void AssertFinite(BoundingBox2D bounds)
    {
        Assert.True(double.IsFinite(bounds.MinX));
        Assert.True(double.IsFinite(bounds.MinY));
        Assert.True(double.IsFinite(bounds.MaxX));
        Assert.True(double.IsFinite(bounds.MaxY));
    }
}
