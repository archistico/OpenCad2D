using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class RadiusDimensionEntityTests
{
    [Fact]
    public void Constructor_ShouldMeasureRadius()
    {
        var dimension = new RadiusDimensionEntity(
            new Point2D(10, 10),
            new Point2D(25, 10),
            new Point2D(32, 14));

        Assert.Equal(EntityKind.RadiusDimension, dimension.Kind);
        Assert.Equal(15, dimension.MeasurementValue);
        Assert.Equal(DimensionStyleId.Standard, dimension.DimensionStyleId);
    }

    [Fact]
    public void Constructor_WithCenterEqualToCirclePoint_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new RadiusDimensionEntity(
            new Point2D(10, 10),
            new Point2D(10, 10),
            new Point2D(20, 10)));
    }

    [Fact]
    public void Transform_WithTranslation_ShouldMoveAllPointsAndKeepId()
    {
        EntityId id = EntityId.New();
        var dimension = new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2),
            id: id);

        var transformed = Assert.IsType<RadiusDimensionEntity>(dimension.Transform(
            Matrix2D.Translation(2, 3)));

        Assert.Equal(id, transformed.Id);
        Assert.Equal(new Point2D(2, 3), transformed.Center);
        Assert.Equal(new Point2D(12, 3), transformed.PointOnCircle);
        Assert.Equal(new Point2D(16, 5), transformed.TextPoint);
    }
}
