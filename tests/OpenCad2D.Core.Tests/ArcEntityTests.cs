using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class ArcEntityTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        var entity = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Assert.Equal(EntityKind.Arc, entity.Kind);
        Assert.NotEqual(EntityId.Empty, entity.Id);
        Assert.Equal(LayerId.Default, entity.LayerId);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnArcBounds()
    {
        var entity = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        BoundingBox2D box = entity.GetBoundingBox();

        Assert.Equal(0, box.MinX, precision: 10);
        Assert.Equal(0, box.MinY, precision: 10);
        Assert.Equal(10, box.MaxX, precision: 10);
        Assert.Equal(10, box.MaxY, precision: 10);
    }

    [Fact]
    public void DistanceTo_ShouldReturnDistanceFromPointToArc()
    {
        var entity = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        double distance = entity.DistanceTo(new Point2D(20, 0));

        Assert.Equal(10, distance, precision: 10);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldReturnMovedArcWithSameId()
    {
        var id = EntityId.New();

        var entity = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90),
            id: id);

        var matrix = Matrix2D.Translation(5, 2);

        var transformed = (ArcEntity)entity.Transform(matrix);

        Assert.Equal(id, transformed.Id);
        Assert.Equal(new Point2D(5, 2), transformed.Center);
        Assert.Equal(10, transformed.Radius, precision: 10);
    }
}