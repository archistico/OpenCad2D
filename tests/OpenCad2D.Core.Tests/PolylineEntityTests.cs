using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class PolylineEntityTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        var entity = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        Assert.Equal(EntityKind.Polyline, entity.Kind);
        Assert.NotEqual(EntityId.Empty, entity.Id);
        Assert.Equal(LayerId.Default, entity.LayerId);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnPolylineBounds()
    {
        var entity = new PolylineEntity(new[]
        {
            new Point2D(5, 10),
            new Point2D(-3, 20),
            new Point2D(15, -2)
        });

        BoundingBox2D box = entity.GetBoundingBox();

        Assert.Equal(-3, box.MinX);
        Assert.Equal(-2, box.MinY);
        Assert.Equal(15, box.MaxX);
        Assert.Equal(20, box.MaxY);
    }

    [Fact]
    public void DistanceTo_ShouldReturnDistanceFromPointToPolyline()
    {
        var entity = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        double distance = entity.DistanceTo(new Point2D(13, 6));

        Assert.Equal(3, distance, precision: 10);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldReturnMovedPolylineWithSameId()
    {
        var id = EntityId.New();

        var entity = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            id: id);

        var matrix = Matrix2D.Translation(5, 2);

        var transformed = (PolylineEntity)entity.Transform(matrix);

        Assert.Equal(id, transformed.Id);
        Assert.Equal(new Point2D(5, 2), transformed.Vertices[0]);
        Assert.Equal(new Point2D(15, 2), transformed.Vertices[1]);
    }
}