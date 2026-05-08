using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class Vector2DTests
{
    [Fact]
    public void Length_ShouldReturnCorrectLength()
    {
        var vector = new Vector2D(3, 4);

        Assert.Equal(5, vector.Length);
    }

    [Fact]
    public void Normalize_ShouldReturnUnitVector()
    {
        var vector = new Vector2D(10, 0);

        var result = vector.Normalize();

        Assert.Equal(1, result.Length, precision: 10);
        Assert.Equal(new Vector2D(1, 0), result);
    }

    [Fact]
    public void Normalize_WithZeroVector_ShouldThrow()
    {
        var vector = Vector2D.Zero;

        Assert.Throws<InvalidOperationException>(() => vector.Normalize());
    }

    [Fact]
    public void Dot_ShouldReturnCorrectValue()
    {
        var first = new Vector2D(1, 2);
        var second = new Vector2D(3, 4);

        double result = first.Dot(second);

        Assert.Equal(11, result);
    }

    [Fact]
    public void Cross_ShouldReturnCorrectValue()
    {
        var first = new Vector2D(1, 0);
        var second = new Vector2D(0, 1);

        double result = first.Cross(second);

        Assert.Equal(1, result);
    }
}