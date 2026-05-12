using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class AngularDimensionEntityTests
{
    [Fact]
    public void Constructor_WithCounterClockwiseMinorAngle_ShouldMeasureSweep()
    {
        var dimension = new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, 8),
            isCounterClockwise: true);

        Assert.Equal(EntityKind.AngularDimension, dimension.Kind);
        Assert.Equal(90, dimension.MeasurementValue, precision: 10);
        Assert.True(dimension.IsCounterClockwise);
    }

    [Fact]
    public void Constructor_WithClockwiseReflexAngle_ShouldMeasureSweepGreaterThan180()
    {
        var dimension = new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, -8),
            isCounterClockwise: false);

        Assert.Equal(270, dimension.MeasurementValue, precision: 10);
        Assert.False(dimension.IsCounterClockwise);
    }

    [Fact]
    public void ShouldUseCounterClockwiseSweep_WhenArcPointIsInsideCounterClockwiseSector_ShouldReturnTrue()
    {
        bool result = AngularDimensionEntity.ShouldUseCounterClockwiseSweep(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, 8));

        Assert.True(result);
    }

    [Fact]
    public void ShouldUseCounterClockwiseSweep_WhenArcPointIsOutsideCounterClockwiseSector_ShouldReturnFalse()
    {
        bool result = AngularDimensionEntity.ShouldUseCounterClockwiseSweep(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, -8));

        Assert.False(result);
    }

    [Fact]
    public void Transform_ShouldTransformAllDefinitionPoints()
    {
        var dimension = new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, 8),
            isCounterClockwise: true);

        var transformed = Assert.IsType<AngularDimensionEntity>(dimension.Transform(
            Matrix2D.Translation(5, 2)));

        Assert.Equal(new Point2D(5, 2), transformed.Center);
        Assert.Equal(new Point2D(15, 2), transformed.FirstRayPoint);
        Assert.Equal(new Point2D(5, 12), transformed.SecondRayPoint);
        Assert.Equal(new Point2D(13, 10), transformed.ArcPoint);
        Assert.True(transformed.IsCounterClockwise);
    }

    [Fact]
    public void WithLayer_ShouldPreserveAngularData()
    {
        var dimension = new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, -8),
            isCounterClockwise: false);

        var changed = Assert.IsType<AngularDimensionEntity>(dimension.WithLayer(new LayerId("Dims")));

        Assert.Equal(new LayerId("Dims"), changed.LayerId);
        Assert.False(changed.IsCounterClockwise);
        Assert.Equal(270, changed.MeasurementValue, precision: 10);
    }
}
