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


    [Fact]
    public void WithFilePath_ShouldRelinkExternalReferenceWithoutChangingGeometry()
    {
        var entity = new ImageReferenceEntity(
            "old.png",
            new Point2D(1, 2),
            new Vector2D(3, 0),
            new Vector2D(0, 4),
            pixelWidth: 300,
            pixelHeight: 400);

        ImageReferenceEntity relinked = entity.WithFilePath(
            "new.jpg",
            pixelWidth: 1200,
            pixelHeight: 800);

        Assert.Equal("new.jpg", relinked.FilePath);
        Assert.Equal(entity.Origin, relinked.Origin);
        Assert.Equal(entity.WidthVector, relinked.WidthVector);
        Assert.Equal(entity.HeightVector, relinked.HeightVector);
        Assert.Equal(1200, relinked.PixelWidth);
        Assert.Equal(800, relinked.PixelHeight);
    }

    [Fact]
    public void WithRotationDegrees_ShouldRotateAroundCenter()
    {
        var entity = new ImageReferenceEntity(
            "plan.png",
            Point2D.Origin,
            new Vector2D(10, 0),
            new Vector2D(0, 4));

        ImageReferenceEntity rotated = entity.WithRotationDegrees(90);

        Assert.Equal(entity.Center.X, rotated.Center.X, precision: 6);
        Assert.Equal(entity.Center.Y, rotated.Center.Y, precision: 6);
        Assert.Equal(10, rotated.Width, precision: 6);
        Assert.Equal(4, rotated.Height, precision: 6);
        Assert.Equal(0, rotated.WidthVector.X, precision: 6);
        Assert.Equal(10, rotated.WidthVector.Y, precision: 6);
        Assert.Equal(-4, rotated.HeightVector.X, precision: 6);
        Assert.Equal(0, rotated.HeightVector.Y, precision: 6);
    }

    [Fact]
    public void WithSize_ShouldPreserveVectorDirections()
    {
        var entity = new ImageReferenceEntity(
            "plan.png",
            Point2D.Origin,
            new Vector2D(0, 10),
            new Vector2D(-4, 0));

        ImageReferenceEntity resized = entity.WithSize(20, 8);

        Assert.Equal(new Vector2D(0, 20), resized.WidthVector);
        Assert.Equal(new Vector2D(-8, 0), resized.HeightVector);
    }

}
