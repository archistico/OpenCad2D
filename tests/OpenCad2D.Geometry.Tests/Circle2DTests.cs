using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class Circle2DTests
{
    [Fact]
    public void Constructor_WithInvalidRadius_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Circle2D(new Point2D(0, 0), 0));
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnCorrectBounds()
    {
        var circle = new Circle2D(
            new Point2D(10, 20),
            5);

        BoundingBox2D box = circle.GetBoundingBox();

        Assert.Equal(5, box.MinX);
        Assert.Equal(15, box.MinY);
        Assert.Equal(15, box.MaxX);
        Assert.Equal(25, box.MaxY);
    }

    [Fact]
    public void PointAt_WithZeroDegrees_ShouldReturnRightPoint()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        Point2D point = circle.PointAt(Angle.FromDegrees(0));

        Assert.Equal(10, point.X, precision: 10);
        Assert.Equal(0, point.Y, precision: 10);
    }

    [Fact]
    public void PointAt_With90Degrees_ShouldReturnTopPoint()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        Point2D point = circle.PointAt(Angle.FromDegrees(90));

        Assert.Equal(0, point.X, precision: 10);
        Assert.Equal(10, point.Y, precision: 10);
    }

    [Fact]
    public void Contains_WithPointOnCircle_ShouldReturnTrue()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        bool result = circle.Contains(new Point2D(10, 0));

        Assert.True(result);
    }

    [Fact]
    public void Contains_WithPointInsideCircle_ShouldReturnFalse()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        bool result = circle.Contains(new Point2D(5, 0));

        Assert.False(result);
    }
}