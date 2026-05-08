using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class LineEntityTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        Assert.Equal(EntityKind.Line, entity.Kind);
        Assert.NotEqual(EntityId.Empty, entity.Id);
        Assert.Equal(LayerId.Default, entity.LayerId);
        Assert.True(entity.IsVisible);
        Assert.False(entity.IsLocked);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnLineBounds()
    {
        var entity = new LineEntity(
            new Point2D(10, -5),
            new Point2D(-2, 20));

        BoundingBox2D box = entity.GetBoundingBox();

        Assert.Equal(-2, box.MinX);
        Assert.Equal(-5, box.MinY);
        Assert.Equal(10, box.MaxX);
        Assert.Equal(20, box.MaxY);
    }

    [Fact]
    public void DistanceTo_ShouldReturnDistanceFromPointToLineSegment()
    {
        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        double distance = entity.DistanceTo(new Point2D(5, 3));

        Assert.Equal(3, distance, precision: 10);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldReturnMovedLineWithSameId()
    {
        var id = EntityId.New();

        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            id);

        var matrix = Matrix2D.Translation(5, 2);

        var transformed = (LineEntity)entity.Transform(matrix);

        Assert.Equal(id, transformed.Id);
        Assert.Equal(new Point2D(5, 2), transformed.Start);
        Assert.Equal(new Point2D(15, 2), transformed.End);
    }
}