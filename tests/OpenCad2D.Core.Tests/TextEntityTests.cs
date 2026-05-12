using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class TextEntityTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        var entity = new TextEntity(new Point2D(10, 20), "Room 101");

        Assert.Equal(EntityKind.Text, entity.Kind);
        Assert.Equal(new Point2D(10, 20), entity.InsertionPoint);
        Assert.Equal("Room 101", entity.Text);
        Assert.Equal(0, entity.RotationDegrees);
        Assert.Equal(TextFormatId.Standard, entity.TextFormatId);
        Assert.NotEqual(EntityId.Empty, entity.Id);
        Assert.Equal(LayerId.Default, entity.LayerId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyText_ShouldThrow(string? text)
    {
        Assert.Throws<ArgumentException>(() => new TextEntity(new Point2D(0, 0), text!));
    }

    [Theory]
    [InlineData(450, 90)]
    [InlineData(-90, 270)]
    public void Constructor_ShouldNormalizeRotation(double input, double expected)
    {
        var entity = new TextEntity(new Point2D(0, 0), "A", input);

        Assert.Equal(expected, entity.RotationDegrees, precision: 10);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnEstimatedBounds()
    {
        var entity = new TextEntity(new Point2D(1, 2), "ABCD");

        BoundingBox2D bounds = entity.GetBoundingBox();

        Assert.True(bounds.Width > 0);
        Assert.True(bounds.Height > 0);
        Assert.Equal(1, bounds.MinX, precision: 10);
        Assert.Equal(2, bounds.MinY, precision: 10);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldMoveInsertionPointAndPreserveTextData()
    {
        EntityId id = EntityId.New();
        LayerId layerId = new("Annotations");

        var entity = new TextEntity(
            new Point2D(1, 2),
            "Door",
            30,
            TextFormatId.Annotation,
            id,
            layerId,
            isVisible: false,
            isLocked: true,
            drawOrder: 8);

        var transformed = Assert.IsType<TextEntity>(
            entity.Transform(Matrix2D.Translation(5, -3)));

        Assert.Equal(new Point2D(6, -1), transformed.InsertionPoint);
        Assert.Equal("Door", transformed.Text);
        Assert.Equal(30, transformed.RotationDegrees);
        Assert.Equal(TextFormatId.Annotation, transformed.TextFormatId);
        Assert.Equal(id, transformed.Id);
        Assert.Equal(layerId, transformed.LayerId);
        Assert.False(transformed.IsVisible);
        Assert.True(transformed.IsLocked);
        Assert.Equal(8, transformed.DrawOrder);
    }

    [Fact]
    public void WithLayer_ShouldPreserveTextDataAndAssignLayer()
    {
        var entity = new TextEntity(new Point2D(4, 5), "Note", textFormatId: TextFormatId.Small);
        LayerId targetLayer = new("Notes");

        var updated = Assert.IsType<TextEntity>(entity.WithLayer(targetLayer));

        Assert.Equal(entity.Id, updated.Id);
        Assert.Equal(targetLayer, updated.LayerId);
        Assert.Equal(entity.Text, updated.Text);
        Assert.Equal(entity.TextFormatId, updated.TextFormatId);
    }
}
