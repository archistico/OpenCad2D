using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class LineSegment2DTests
{
    [Fact]
    public void Length_ShouldReturnCorrectLength()
    {
        var segment = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(3, 4));

        Assert.Equal(5, segment.Length);
    }

    [Fact]
    public void Midpoint_ShouldReturnCenterPoint()
    {
        var segment = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 20));

        Assert.Equal(new Point2D(5, 10), segment.Midpoint);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnCorrectBounds()
    {
        var segment = new LineSegment2D(
            new Point2D(10, -5),
            new Point2D(-2, 20));

        var box = segment.GetBoundingBox();

        Assert.Equal(-2, box.MinX);
        Assert.Equal(-5, box.MinY);
        Assert.Equal(10, box.MaxX);
        Assert.Equal(20, box.MaxY);
    }
}