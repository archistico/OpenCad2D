using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Geometry.Tests;

public sealed class Matrix2DTests
{
    [Fact]
    public void Translation_ShouldMovePoint()
    {
        var matrix = Matrix2D.Translation(10, -5);
        var point = new Point2D(1, 2);

        var result = matrix.Transform(point);

        Assert.Equal(new Point2D(11, -3), result);
    }

    [Fact]
    public void Scale_ShouldScalePointAroundOrigin()
    {
        var matrix = Matrix2D.Scale(2, Point2D.Origin);
        var point = new Point2D(3, 4);

        var result = matrix.Transform(point);

        Assert.Equal(new Point2D(6, 8), result);
    }

    [Fact]
    public void Scale_ShouldScalePointAroundCenter()
    {
        var matrix = Matrix2D.Scale(2, new Point2D(10, 10));
        var point = new Point2D(11, 10);

        var result = matrix.Transform(point);

        Assert.Equal(new Point2D(12, 10), result);
    }

    [Fact]
    public void Rotation_90DegreesAroundOrigin_ShouldRotatePoint()
    {
        var matrix = Matrix2D.Rotation(Math.PI / 2, Point2D.Origin);
        var point = new Point2D(1, 0);

        var result = matrix.Transform(point);

        Assert.Equal(0, result.X, precision: 10);
        Assert.Equal(1, result.Y, precision: 10);
    }
}