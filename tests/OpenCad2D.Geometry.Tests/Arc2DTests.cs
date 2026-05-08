using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class Arc2DTests
{
    [Fact]
    public void Constructor_WithInvalidRadius_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Arc2D(
                new Point2D(0, 0),
                0,
                Angle.FromDegrees(0),
                Angle.FromDegrees(90)));
    }

    [Fact]
    public void StartPoint_ShouldReturnCorrectPoint()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Point2D point = arc.StartPoint;

        Assert.Equal(10, point.X, precision: 10);
        Assert.Equal(0, point.Y, precision: 10);
    }

    [Fact]
    public void EndPoint_ShouldReturnCorrectPoint()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Point2D point = arc.EndPoint;

        Assert.Equal(0, point.X, precision: 10);
        Assert.Equal(10, point.Y, precision: 10);
    }

    [Fact]
    public void ContainsAngle_ForCounterClockwiseArc_ShouldReturnTrueForAngleInside()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        bool result = arc.ContainsAngle(Angle.FromDegrees(45));

        Assert.True(result);
    }

    [Fact]
    public void ContainsAngle_ForCounterClockwiseArc_ShouldReturnFalseForAngleOutside()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        bool result = arc.ContainsAngle(Angle.FromDegrees(180));

        Assert.False(result);
    }

    [Fact]
    public void ContainsAngle_ForCounterClockwiseArcCrossingZero_ShouldReturnTrue()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(350),
            Angle.FromDegrees(10));

        bool result = arc.ContainsAngle(Angle.FromDegrees(0));

        Assert.True(result);
    }

    [Fact]
    public void ContainsPoint_WithPointOnArc_ShouldReturnTrue()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        bool result = arc.ContainsPoint(new Point2D(0, 10));

        Assert.True(result);
    }

    [Fact]
    public void ContainsPoint_WithPointOnCircleButOutsideArc_ShouldReturnFalse()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        bool result = arc.ContainsPoint(new Point2D(-10, 0));

        Assert.False(result);
    }

    [Fact]
    public void GetBoundingBox_ForQuarterArc_ShouldReturnCorrectBounds()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        BoundingBox2D box = arc.GetBoundingBox();

        Assert.Equal(0, box.MinX, precision: 10);
        Assert.Equal(0, box.MinY, precision: 10);
        Assert.Equal(10, box.MaxX, precision: 10);
        Assert.Equal(10, box.MaxY, precision: 10);
    }

    [Fact]
    public void GetBoundingBox_ForHalfArc_ShouldReturnCorrectBounds()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        BoundingBox2D box = arc.GetBoundingBox();

        Assert.Equal(-10, box.MinX, precision: 10);
        Assert.Equal(0, box.MinY, precision: 10);
        Assert.Equal(10, box.MaxX, precision: 10);
        Assert.Equal(10, box.MaxY, precision: 10);
    }
}