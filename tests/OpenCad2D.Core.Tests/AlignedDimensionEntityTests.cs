using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class AlignedDimensionEntityTests
{
    [Fact]
    public void Constructor_ShouldMeasureDistanceBetweenPoints()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(0, 5));

        Assert.Equal(EntityKind.AlignedDimension, dimension.Kind);
        Assert.Equal(5, dimension.MeasurementValue, precision: 10);
        Assert.Equal(DimensionStyleId.Standard, dimension.DimensionStyleId);
    }

    [Fact]
    public void Constructor_WithEqualMeasuredPoints_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new AlignedDimensionEntity(
                new Point2D(1, 1),
                new Point2D(1, 1),
                new Point2D(0, 5)));
    }

    [Fact]
    public void Transform_WithTranslation_ShouldMoveAllPointsAndKeepId()
    {
        EntityId id = EntityId.New();
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(0, 5),
            id: id);

        var transformed = Assert.IsType<AlignedDimensionEntity>(dimension.Transform(
            Matrix2D.Translation(2, 3)));

        Assert.Equal(id, transformed.Id);
        Assert.Equal(new Point2D(2, 3), transformed.FirstPoint);
        Assert.Equal(new Point2D(5, 7), transformed.SecondPoint);
        Assert.Equal(new Point2D(2, 8), transformed.DimensionLinePoint);
    }

    [Fact]
    public void GetClosestPoint_ShouldReturnPointOnDimensionLine()
    {
        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 5));

        Point2D closest = dimension.GetClosestPoint(new Point2D(3, 10));

        Assert.Equal(3, closest.X, precision: 10);
        Assert.Equal(5, closest.Y, precision: 10);
    }
}
