using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class BoundingBox2DTests
{
    [Fact]
    public void Contains_PointInside_ShouldReturnTrue()
    {
        var box = new BoundingBox2D(0, 0, 10, 10);

        bool result = box.Contains(new Point2D(5, 5));

        Assert.True(result);
    }

    [Fact]
    public void Contains_PointOutside_ShouldReturnFalse()
    {
        var box = new BoundingBox2D(0, 0, 10, 10);

        bool result = box.Contains(new Point2D(20, 5));

        Assert.False(result);
    }

    [Fact]
    public void Intersects_OverlappingBoxes_ShouldReturnTrue()
    {
        var first = new BoundingBox2D(0, 0, 10, 10);
        var second = new BoundingBox2D(5, 5, 15, 15);

        Assert.True(first.Intersects(second));
    }

    [Fact]
    public void Intersects_SeparatedBoxes_ShouldReturnFalse()
    {
        var first = new BoundingBox2D(0, 0, 10, 10);
        var second = new BoundingBox2D(20, 20, 30, 30);

        Assert.False(first.Intersects(second));
    }

    [Fact]
    public void GetEdges_ShouldReturnFourEdges()
    {
        var box = new BoundingBox2D(0, 0, 10, 20);

        IReadOnlyList<LineSegment2D> edges = box.GetEdges();

        Assert.Equal(4, edges.Count);

        Assert.Equal(new Point2D(0, 0), edges[0].Start);
        Assert.Equal(new Point2D(10, 0), edges[0].End);

        Assert.Equal(new Point2D(10, 0), edges[1].Start);
        Assert.Equal(new Point2D(10, 20), edges[1].End);

        Assert.Equal(new Point2D(10, 20), edges[2].Start);
        Assert.Equal(new Point2D(0, 20), edges[2].End);

        Assert.Equal(new Point2D(0, 20), edges[3].Start);
        Assert.Equal(new Point2D(0, 0), edges[3].End);
    }
}