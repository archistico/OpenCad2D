using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class DistanceServiceTests
{
    [Fact]
    public void ClosestPointOnSegment_WhenProjectionIsInsideSegment_ShouldReturnProjectedPoint()
    {
        var point = new Point2D(5, 5);
        var segment = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        Point2D result = DistanceService.ClosestPointOnSegment(point, segment);

        Assert.Equal(new Point2D(5, 0), result);
    }

    [Fact]
    public void ClosestPointOnSegment_WhenProjectionIsBeforeStart_ShouldReturnStart()
    {
        var point = new Point2D(-5, 3);
        var segment = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        Point2D result = DistanceService.ClosestPointOnSegment(point, segment);

        Assert.Equal(new Point2D(0, 0), result);
    }

    [Fact]
    public void ClosestPointOnSegment_WhenProjectionIsAfterEnd_ShouldReturnEnd()
    {
        var point = new Point2D(15, 3);
        var segment = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        Point2D result = DistanceService.ClosestPointOnSegment(point, segment);

        Assert.Equal(new Point2D(10, 0), result);
    }

    [Fact]
    public void DistancePointToSegment_ShouldReturnCorrectDistance()
    {
        var point = new Point2D(5, 4);
        var segment = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        double result = DistanceService.DistancePointToSegment(point, segment);

        Assert.Equal(4, result);
    }

    [Fact]
    public void ClosestPointOnLine_ShouldReturnProjectedPoint()
    {
        var point = new Point2D(5, 5);
        var line = Line2D.FromPoints(
            new Point2D(0, 0),
            new Point2D(10, 0));

        Point2D result = DistanceService.ClosestPointOnLine(point, line);

        Assert.Equal(new Point2D(5, 0), result);
    }

    [Fact]
    public void DistancePointToLine_ShouldReturnCorrectDistance()
    {
        var point = new Point2D(5, 4);
        var line = Line2D.FromPoints(
            new Point2D(0, 0),
            new Point2D(10, 0));

        double result = DistanceService.DistancePointToLine(point, line);

        Assert.Equal(4, result);
    }
}