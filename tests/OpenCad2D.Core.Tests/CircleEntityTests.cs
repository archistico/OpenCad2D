using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class CircleEntityTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        var entity = new CircleEntity(
            new Point2D(0, 0),
            10);

        Assert.Equal(EntityKind.Circle, entity.Kind);
        Assert.NotEqual(EntityId.Empty, entity.Id);
        Assert.Equal(LayerId.Default, entity.LayerId);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnCircleBounds()
    {
        var entity = new CircleEntity(
            new Point2D(10, 20),
            5);

        BoundingBox2D box = entity.GetBoundingBox();

        Assert.Equal(5, box.MinX);
        Assert.Equal(15, box.MinY);
        Assert.Equal(15, box.MaxX);
        Assert.Equal(25, box.MaxY);
    }

    [Fact]
    public void DistanceTo_ShouldReturnDistanceFromPointToCircle()
    {
        var entity = new CircleEntity(
            new Point2D(0, 0),
            10);

        double distance = entity.DistanceTo(new Point2D(15, 0));

        Assert.Equal(5, distance, precision: 10);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldReturnMovedCircleWithSameId()
    {
        var id = EntityId.New();

        var entity = new CircleEntity(
            new Point2D(0, 0),
            10,
            id);

        var matrix = Matrix2D.Translation(5, 2);

        var transformed = (CircleEntity)entity.Transform(matrix);

        Assert.Equal(id, transformed.Id);
        Assert.Equal(new Point2D(5, 2), transformed.Center);
        Assert.Equal(10, transformed.Radius, precision: 10);
    }

    [Fact]
    public void Transform_WithUniformScale_ShouldScaleRadius()
    {
        var entity = new CircleEntity(
            new Point2D(0, 0),
            10);

        var matrix = Matrix2D.Scale(2, Point2D.Origin);

        var transformed = (CircleEntity)entity.Transform(matrix);

        Assert.Equal(20, transformed.Radius, precision: 10);
    }
}