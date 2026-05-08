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

    [Fact]
    public void ClosestPointOnCircle_WithExternalPoint_ShouldReturnPointOnCircle()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        Point2D point = new(20, 0);

        Point2D result = DistanceService.ClosestPointOnCircle(point, circle);

        Assert.Equal(10, result.X, precision: 10);
        Assert.Equal(0, result.Y, precision: 10);
    }

    [Fact]
    public void ClosestPointOnCircle_WithPointAtCenter_ShouldReturnRightPoint()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        Point2D point = new(0, 0);

        Point2D result = DistanceService.ClosestPointOnCircle(point, circle);

        Assert.Equal(10, result.X, precision: 10);
        Assert.Equal(0, result.Y, precision: 10);
    }

    [Fact]
    public void DistancePointToCircle_WithExternalPoint_ShouldReturnCorrectDistance()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        Point2D point = new(15, 0);

        double result = DistanceService.DistancePointToCircle(point, circle);

        Assert.Equal(5, result, precision: 10);
    }

    [Fact]
    public void DistancePointToCircle_WithInternalPoint_ShouldReturnCorrectDistance()
    {
        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        Point2D point = new(6, 0);

        double result = DistanceService.DistancePointToCircle(point, circle);

        Assert.Equal(4, result, precision: 10);
    }

    [Fact]
    public void ClosestPointOnArc_WhenProjectionFallsInsideArc_ShouldReturnProjectedPoint()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Point2D point = new(20, 20);

        Point2D result = DistanceService.ClosestPointOnArc(point, arc);

        double expected = Math.Sqrt(50);

        Assert.Equal(expected, result.X, precision: 10);
        Assert.Equal(expected, result.Y, precision: 10);
    }

    [Fact]
    public void ClosestPointOnArc_WhenProjectionFallsOutsideArc_ShouldReturnNearestEndpoint()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Point2D point = new(-20, 0);

        Point2D result = DistanceService.ClosestPointOnArc(point, arc);

        Assert.Equal(0, result.X, precision: 10);
        Assert.Equal(10, result.Y, precision: 10);
    }

    [Fact]
    public void ClosestPointOnArc_WithPointAtCenter_ShouldReturnStartPoint()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Point2D point = new(0, 0);

        Point2D result = DistanceService.ClosestPointOnArc(point, arc);

        Assert.Equal(10, result.X, precision: 10);
        Assert.Equal(0, result.Y, precision: 10);
    }

    [Fact]
    public void DistancePointToArc_WhenProjectionFallsInsideArc_ShouldReturnCorrectDistance()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Point2D point = new(20, 0);

        double result = DistanceService.DistancePointToArc(point, arc);

        Assert.Equal(10, result, precision: 10);
    }

    [Fact]
    public void DistancePointToArc_WhenProjectionFallsOutsideArc_ShouldReturnDistanceToEndpoint()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        Point2D point = new(-10, 0);

        double result = DistanceService.DistancePointToArc(point, arc);

        double expected = Math.Sqrt(200);

        Assert.Equal(expected, result, precision: 10);
    }
}