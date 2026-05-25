using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class ImageReferenceEntityTests
{
    [Fact]
    public void Kind_ShouldBeImageReference()
    {
        var entity = new ImageReferenceEntity(
            "plan.png",
            Point2D.Origin,
            new Vector2D(10, 0),
            new Vector2D(0, 5));

        Assert.Equal(EntityKind.ImageReference, entity.Kind);
    }

    [Fact]
    public void GetBoundingBox_ShouldIncludeAllRotatedCorners()
    {
        var entity = new ImageReferenceEntity(
            "plan.jpg",
            new Point2D(2, 3),
            new Vector2D(10, 0),
            new Vector2D(0, 5));

        CadEntity rotated = entity.Transform(
            Matrix2D.Rotation(Math.PI / 2.0, entity.Origin));

        BoundingBox2D box = rotated.GetBoundingBox();

        Assert.Equal(-3, box.MinX, precision: 6);
        Assert.Equal(3, box.MinY, precision: 6);
        Assert.Equal(2, box.MaxX, precision: 6);
        Assert.Equal(13, box.MaxY, precision: 6);
    }

    [Fact]
    public void WithLayer_ShouldKeepExternalReferenceAndGeometry()
    {
        var entity = new ImageReferenceEntity(
            "scan.png",
            new Point2D(1, 2),
            new Vector2D(3, 0),
            new Vector2D(0, 4),
            pixelWidth: 300,
            pixelHeight: 400);

        var moved = Assert.IsType<ImageReferenceEntity>(entity.WithLayer(new LayerId("Images")));

        Assert.Equal("scan.png", moved.FilePath);
        Assert.Equal(new LayerId("Images"), moved.LayerId);
        Assert.Equal(entity.Origin, moved.Origin);
        Assert.Equal(entity.WidthVector, moved.WidthVector);
        Assert.Equal(entity.HeightVector, moved.HeightVector);
        Assert.Equal(300, moved.PixelWidth);
        Assert.Equal(400, moved.PixelHeight);
    }
}
