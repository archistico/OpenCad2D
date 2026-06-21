using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class WindowEntityTests
{
    [Fact]
    public void Constructor_ShouldInitializeParametricWindow()
    {
        var window = new WindowEntity(
            new Point2D(10, 20),
            width: 120,
            wallThickness: 20,
            frameOffset: 4,
            anchor: AnchorPoint.MiddleLeft);

        Assert.Equal(EntityKind.Window, window.Kind);
        Assert.Equal(new Point2D(10, 20), window.InsertionPoint);
        Assert.Equal(120, window.Width);
        Assert.Equal(20, window.WallThickness);
        Assert.Equal(4, window.FrameOffset);
        Assert.Equal(AnchorPoint.MiddleLeft, window.Anchor);
        Assert.True(window.MaskWallOpening);
    }

    [Fact]
    public void GetGeneratedGeometry_ShouldExposeSchematicWindowSegments()
    {
        var window = new WindowEntity(
            Point2D.Origin,
            width: 120,
            wallThickness: 20,
            frameOffset: 4);

        var geometry = window.GetGeneratedGeometry();

        Assert.Equal(7, geometry.Segments.Count);
        Assert.True(geometry.HasWallMask);
        Assert.Equal(4, geometry.WallMaskPolygon.Count);
    }

    [Fact]
    public void GetGeneratedGeometry_WithMaskDisabled_ShouldNotExposeWallMaskPolygon()
    {
        var window = new WindowEntity(
            Point2D.Origin,
            width: 120,
            wallThickness: 20,
            frameOffset: 4,
            maskWallOpening: false);

        var geometry = window.GetGeneratedGeometry();

        Assert.False(geometry.HasWallMask);
        Assert.Empty(geometry.WallMaskPolygon);
    }

    [Fact]
    public void Transform_ShouldPreserveWindowParametersAndScaleDimensions()
    {
        var window = new WindowEntity(
            new Point2D(10, 20),
            width: 120,
            wallThickness: 20,
            frameOffset: 4,
            anchor: AnchorPoint.BottomCenter,
            maskWallOpening: false);

        WindowEntity transformed = Assert.IsType<WindowEntity>(
            window.Transform(Matrix2D.Scale(2.0, Point2D.Origin)));

        Assert.Equal(new Point2D(20, 40), transformed.InsertionPoint);
        Assert.Equal(240, transformed.Width);
        Assert.Equal(40, transformed.WallThickness);
        Assert.Equal(8, transformed.FrameOffset);
        Assert.Equal(window.Anchor, transformed.Anchor);
        Assert.False(transformed.MaskWallOpening);
    }

    [Fact]
    public void Constructor_WithInvalidFrameOffset_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WindowEntity(
            Point2D.Origin,
            width: 120,
            wallThickness: 20,
            frameOffset: 15));
    }
}
