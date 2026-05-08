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

    [Fact]
    public void ClosestPointOnPolyline_ShouldReturnClosestPointOnNearestSegment()
    {
        var polyline = new Polyline2D(new[]
        {
        new Point2D(0, 0),
        new Point2D(10, 0),
        new Point2D(10, 10)
    });

        Point2D point = new(7, 2);

        Point2D result = DistanceService.ClosestPointOnPolyline(point, polyline);

        Assert.Equal(7, result.X, precision: 10);
        Assert.Equal(0, result.Y, precision: 10);
    }

    [Fact]
    public void ClosestPointOnPolyline_WithPointNearSecondSegment_ShouldReturnClosestPointOnSecondSegment()
    {
        var polyline = new Polyline2D(new[]
        {
        new Point2D(0, 0),
        new Point2D(10, 0),
        new Point2D(10, 10)
    });

        Point2D point = new(13, 6);

        Point2D result = DistanceService.ClosestPointOnPolyline(point, polyline);

        Assert.Equal(10, result.X, precision: 10);
        Assert.Equal(6, result.Y, precision: 10);
    }

    [Fact]
    public void DistancePointToPolyline_ShouldReturnCorrectDistance()
    {
        var polyline = new Polyline2D(new[]
        {
        new Point2D(0, 0),
        new Point2D(10, 0),
        new Point2D(10, 10)
    });

        Point2D point = new(13, 6);

        double result = DistanceService.DistancePointToPolyline(point, polyline);

        Assert.Equal(3, result, precision: 10);
    }

    [Fact]
    public void ClosestPointOnClosedPolyline_ShouldConsiderClosingSegment()
    {
        var polyline = new Polyline2D(
            new[]
            {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
            },
            isClosed: true);

        Point2D point = new(-1, 5);

        Point2D result = DistanceService.ClosestPointOnPolyline(point, polyline);

        // Closing segment is from (10,10) to (0,0).
        // The closest point from (-1,5) to that diagonal is around (2,2).
        Assert.Equal(2, result.X, precision: 10);
        Assert.Equal(2, result.Y, precision: 10);
    }

    [Fact]
    public void DistancePointToBoundingBox_WhenPointIsInside_ShouldReturnZero()
    {
        var box = new BoundingBox2D(0, 0, 10, 10);

        double result = DistanceService.DistancePointToBoundingBox(
            new Point2D(5, 5),
            box);

        Assert.Equal(0, result, precision: 10);
    }

    [Fact]
    public void DistancePointToBoundingBox_WhenPointIsOutsideHorizontally_ShouldReturnHorizontalDistance()
    {
        var box = new BoundingBox2D(0, 0, 10, 10);

        double result = DistanceService.DistancePointToBoundingBox(
            new Point2D(15, 5),
            box);

        Assert.Equal(5, result, precision: 10);
    }

    [Fact]
    public void DistancePointToBoundingBox_WhenPointIsOutsideDiagonally_ShouldReturnDiagonalDistance()
    {
        var box = new BoundingBox2D(0, 0, 10, 10);

        double result = DistanceService.DistancePointToBoundingBox(
            new Point2D(13, 14),
            box);

        Assert.Equal(5, result, precision: 10);
    }
}