using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class Point2DTests
{
    [Fact]
    public void DistanceTo_ShouldReturnCorrectDistance()
    {
        var first = new Point2D(0, 0);
        var second = new Point2D(3, 4);

        double distance = first.DistanceTo(second);

        Assert.Equal(5, distance);
    }

    [Fact]
    public void Translate_ShouldMovePointByVector()
    {
        var point = new Point2D(10, 20);
        var vector = new Vector2D(5, -2);

        var result = point.Translate(vector);

        Assert.Equal(new Point2D(15, 18), result);
    }
}