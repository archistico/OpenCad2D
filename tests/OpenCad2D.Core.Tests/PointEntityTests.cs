using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class PointEntityTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        var entity = new PointEntity(new Point2D(10, 20));

        Assert.Equal(EntityKind.Point, entity.Kind);
        Assert.Equal(new Point2D(10, 20), entity.Position);
        Assert.NotEqual(EntityId.Empty, entity.Id);
        Assert.Equal(LayerId.Default, entity.LayerId);
        Assert.True(entity.IsVisible);
        Assert.False(entity.IsLocked);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnZeroSizedBoundsAtPosition()
    {
        var entity = new PointEntity(new Point2D(-3, 7));

        BoundingBox2D box = entity.GetBoundingBox();

        Assert.Equal(-3, box.MinX);
        Assert.Equal(7, box.MinY);
        Assert.Equal(-3, box.MaxX);
        Assert.Equal(7, box.MaxY);
        Assert.Equal(0, box.Width);
        Assert.Equal(0, box.Height);
    }

    [Fact]
    public void DistanceTo_ShouldReturnDistanceFromPosition()
    {
        var entity = new PointEntity(new Point2D(0, 0));

        double distance = entity.DistanceTo(new Point2D(3, 4));

        Assert.Equal(5, distance, precision: 10);
    }

    [Fact]
    public void GetClosestPoint_ShouldReturnPosition()
    {
        var entity = new PointEntity(new Point2D(12, -4));

        Point2D closest = entity.GetClosestPoint(new Point2D(100, 100));

        Assert.Equal(entity.Position, closest);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldReturnMovedPointWithSameIdentityAndStyle()
    {
        EntityId id = EntityId.New();
        LayerId layerId = new("Details");

        var entity = new PointEntity(
            new Point2D(1, 2),
            id,
            layerId,
            isVisible: false,
            isLocked: true,
            drawOrder: 17);

        var transformed = Assert.IsType<PointEntity>(
            entity.Transform(Matrix2D.Translation(5, -3)));

        Assert.Equal(id, transformed.Id);
        Assert.Equal(layerId, transformed.LayerId);
        Assert.False(transformed.IsVisible);
        Assert.True(transformed.IsLocked);
        Assert.Equal(17, transformed.DrawOrder);
        Assert.Equal(new Point2D(6, -1), transformed.Position);
    }

    [Fact]
    public void WithLayer_ShouldPreserveGeometryAndAssignLayer()
    {
        var entity = new PointEntity(new Point2D(4, 5));
        LayerId targetLayer = new("Annotations");

        var updated = Assert.IsType<PointEntity>(entity.WithLayer(targetLayer));

        Assert.Equal(entity.Id, updated.Id);
        Assert.Equal(targetLayer, updated.LayerId);
        Assert.Equal(entity.Position, updated.Position);
    }
}
