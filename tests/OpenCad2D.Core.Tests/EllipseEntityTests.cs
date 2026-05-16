using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class EllipseEntityTests
{
    [Fact]
    public void Constructor_ShouldExposeEllipseGeometry()
    {
        var entity = new EllipseEntity(
            new Point2D(10, 20),
            new Vector2D(8, 0),
            3);

        Assert.Equal(EntityKind.Ellipse, entity.Kind);
        Assert.Equal(new Point2D(10, 20), entity.Center);
        Assert.Equal(new Vector2D(8, 0), entity.MajorAxis);
        Assert.Equal(8, entity.MajorRadius);
        Assert.Equal(3, entity.MinorRadius);
        Assert.Equal(new Point2D(18, 20), entity.MajorAxisEndPoint);
        Assert.Equal(new Point2D(10, 23), entity.MinorAxisEndPoint);
    }

    [Fact]
    public void GetBoundingBox_ForAxisAlignedEllipse_ShouldUseRadii()
    {
        var entity = new EllipseEntity(
            new Point2D(10, 20),
            new Vector2D(8, 0),
            3);

        BoundingBox2D bounds = entity.GetBoundingBox();

        Assert.Equal(2, bounds.MinX);
        Assert.Equal(17, bounds.MinY);
        Assert.Equal(18, bounds.MaxX);
        Assert.Equal(23, bounds.MaxY);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldMoveCenterAndPreserveRadii()
    {
        var entity = new EllipseEntity(
            new Point2D(10, 20),
            new Vector2D(8, 0),
            3,
            new EntityId(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var moved = Assert.IsType<EllipseEntity>(entity.Transform(Matrix2D.Translation(5, -2)));

        Assert.Equal(new Point2D(15, 18), moved.Center);
        Assert.Equal(new Vector2D(8, 0), moved.MajorAxis);
        Assert.Equal(3, moved.MinorRadius);
        Assert.Equal(entity.Id, moved.Id);
    }

    [Fact]
    public void Constructor_WithInvalidRadii_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EllipseEntity(
            Point2D.Origin,
            Vector2D.Zero,
            3));

        Assert.Throws<ArgumentOutOfRangeException>(() => new EllipseEntity(
            Point2D.Origin,
            new Vector2D(8, 0),
            0));
    }
}
