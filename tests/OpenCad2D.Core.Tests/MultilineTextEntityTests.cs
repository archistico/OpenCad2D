using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class MultilineTextEntityTests
{
    [Fact]
    public void Constructor_ShouldStoreMultilineText()
    {
        var entity = new MultilineTextEntity(
            new Point2D(10, 20),
            "First line\nSecond line",
            textFormatId: TextFormatId.Annotation);

        Assert.Equal(EntityKind.MultilineText, entity.Kind);
        Assert.Equal(new Point2D(10, 20), entity.InsertionPoint);
        Assert.Equal("First line\nSecond line", entity.Text);
        Assert.Equal(new[] { "First line", "Second line" }, entity.Lines);
        Assert.Equal(TextFormatId.Annotation, entity.TextFormatId);
        Assert.Equal(0, entity.ReferenceWidth);
    }

    [Fact]
    public void Constructor_WithReferenceWidth_ShouldStoreValue()
    {
        var entity = new MultilineTextEntity(
            new Point2D(10, 20),
            "First line\nSecond line",
            referenceWidth: 120);

        Assert.Equal(120, entity.ReferenceWidth);
    }

    [Fact]
    public void Constructor_WithNegativeReferenceWidth_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultilineTextEntity(
            new Point2D(0, 0),
            "Note",
            referenceWidth: -1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyText_ShouldThrow(string? text)
    {
        Assert.Throws<ArgumentException>(() => new MultilineTextEntity(new Point2D(0, 0), text!));
    }

    [Fact]
    public void Constructor_ShouldNormalizeWindowsLineEndings()
    {
        var entity = new MultilineTextEntity(new Point2D(0, 0), "A\r\nB\rC");

        Assert.Equal("A\nB\nC", entity.Text);
        Assert.Equal(new[] { "A", "B", "C" }, entity.Lines);
    }

    [Fact]
    public void Transform_ShouldMoveInsertionPointAndRotation()
    {
        var entity = new MultilineTextEntity(new Point2D(1, 2), "A\nB", 10);
        Matrix2D transform = Matrix2D.Translation(3, 4);

        var transformed = Assert.IsType<MultilineTextEntity>(entity.Transform(transform));

        Assert.Equal(new Point2D(4, 6), transformed.InsertionPoint);
        Assert.Equal(10, transformed.RotationDegrees, 6);
        Assert.Equal(entity.Text, transformed.Text);
        Assert.Equal(entity.ReferenceWidth, transformed.ReferenceWidth);
    }

    [Fact]
    public void WithLayer_ShouldPreserveMultilineData()
    {
        var entity = new MultilineTextEntity(new Point2D(1, 2), "Note\nDetails", 15, TextFormatId.Small);
        var layerId = new LayerId("Annotations");

        var updated = Assert.IsType<MultilineTextEntity>(entity.WithLayer(layerId));

        Assert.Equal(layerId, updated.LayerId);
        Assert.Equal(entity.Text, updated.Text);
        Assert.Equal(entity.RotationDegrees, updated.RotationDegrees);
        Assert.Equal(entity.TextFormatId, updated.TextFormatId);
        Assert.Equal(entity.ReferenceWidth, updated.ReferenceWidth);
    }

    [Fact]
    public void WithReferenceWidth_ShouldReturnUpdatedEntity()
    {
        var entity = new MultilineTextEntity(new Point2D(1, 2), "Note");

        MultilineTextEntity updated = entity.WithReferenceWidth(80);

        Assert.Equal(80, updated.ReferenceWidth);
        Assert.Equal(entity.Id, updated.Id);
        Assert.Equal(entity.Text, updated.Text);
    }
}
