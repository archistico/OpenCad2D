using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class ArcCreationServiceTests
{
    [Fact]
    public void TryCreateFromThreePoints_WithValidCounterClockwisePoints_ShouldCreateArc()
    {
        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(-10, 0),
            out Arc2D arc);

        Assert.True(result);
        Assert.Equal(new Point2D(0, 0), arc.Center);
        Assert.Equal(10, arc.Radius, precision: 10);
        Assert.Equal(0, arc.StartAngle.Degrees, precision: 10);
        Assert.Equal(180, arc.EndAngle.Degrees, precision: 10);
        Assert.True(arc.IsCounterClockwise);
    }

    [Fact]
    public void TryCreateFromThreePoints_WithClockwiseMiddlePoint_ShouldCreateClockwiseArc()
    {
        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(10, 0),
            new Point2D(0, -10),
            new Point2D(-10, 0),
            out Arc2D arc);

        Assert.True(result);
        Assert.Equal(new Point2D(0, 0), arc.Center);
        Assert.Equal(10, arc.Radius, precision: 10);
        Assert.Equal(0, arc.StartAngle.Degrees, precision: 10);
        Assert.Equal(180, arc.EndAngle.Degrees, precision: 10);
        Assert.False(arc.IsCounterClockwise);
    }

    [Fact]
    public void TryCreateFromThreePoints_WithNonOriginCircle_ShouldCreateCorrectCenter()
    {
        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(15, 5),
            new Point2D(5, 15),
            new Point2D(-5, 5),
            out Arc2D arc);

        Assert.True(result);
        Assert.Equal(5, arc.Center.X, precision: 10);
        Assert.Equal(5, arc.Center.Y, precision: 10);
        Assert.Equal(10, arc.Radius, precision: 10);
    }

    [Fact]
    public void TryCreateFromThreePoints_ShouldCreateArcThatContainsMiddlePoint()
    {
        var middlePoint = new Point2D(0, 10);

        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(10, 0),
            middlePoint,
            new Point2D(-10, 0),
            out Arc2D arc);

        Assert.True(result);
        Assert.True(arc.ContainsPoint(middlePoint));
    }

    [Fact]
    public void TryCreateFromThreePoints_WithCollinearPoints_ShouldFail()
    {
        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(20, 0),
            out _);

        Assert.False(result);
    }

    [Fact]
    public void TryCreateFromThreePoints_WithDuplicateStartAndMiddle_ShouldFail()
    {
        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            out _);

        Assert.False(result);
    }

    [Fact]
    public void TryCreateFromThreePoints_WithDuplicateMiddleAndEnd_ShouldFail()
    {
        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 0),
            out _);

        Assert.False(result);
    }

    [Fact]
    public void TryCreateFromThreePoints_WithDuplicateStartAndEnd_ShouldFail()
    {
        bool result = ArcCreationService.TryCreateFromThreePoints(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 0),
            out _);

        Assert.False(result);
    }
}
