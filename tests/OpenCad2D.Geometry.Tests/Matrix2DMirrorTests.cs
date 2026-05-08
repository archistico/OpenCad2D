using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Geometry.Tests;

public sealed class Matrix2DMirrorTests
{
    [Fact]
    public void Mirror_AcrossXAxis_ShouldInvertY()
    {
        Line2D mirrorLine = Line2D.FromPoints(
            new Point2D(0, 0),
            new Point2D(10, 0));

        Matrix2D matrix = Matrix2D.Mirror(mirrorLine);

        Point2D result = matrix.Transform(new Point2D(5, 3));

        Assert.Equal(5, result.X, precision: 10);
        Assert.Equal(-3, result.Y, precision: 10);
    }

    [Fact]
    public void Mirror_AcrossYAxis_ShouldInvertX()
    {
        Line2D mirrorLine = Line2D.FromPoints(
            new Point2D(0, 0),
            new Point2D(0, 10));

        Matrix2D matrix = Matrix2D.Mirror(mirrorLine);

        Point2D result = matrix.Transform(new Point2D(5, 3));

        Assert.Equal(-5, result.X, precision: 10);
        Assert.Equal(3, result.Y, precision: 10);
    }

    [Fact]
    public void Mirror_AcrossDiagonalYEqualsX_ShouldSwapCoordinates()
    {
        Line2D mirrorLine = Line2D.FromPoints(
            new Point2D(0, 0),
            new Point2D(10, 10));

        Matrix2D matrix = Matrix2D.Mirror(mirrorLine);

        Point2D result = matrix.Transform(new Point2D(2, 5));

        Assert.Equal(5, result.X, precision: 10);
        Assert.Equal(2, result.Y, precision: 10);
    }
}