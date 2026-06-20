using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class CircleIntersectionServiceTests
{
    [Fact]
    public void IntersectLineCircle_WhenLineCrossesCircle_ShouldReturnTwoPoints()
    {
        var line = Line2D.FromPoints(
            new Point2D(-20, 0),
            new Point2D(20, 0));

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectLineCircle(line, circle);

        Assert.Equal(2, points.Count);

        Assert.Contains(points, point =>
            Math.Abs(point.X - 10) < 1e-9 &&
            Math.Abs(point.Y) < 1e-9);

        Assert.Contains(points, point =>
            Math.Abs(point.X + 10) < 1e-9 &&
            Math.Abs(point.Y) < 1e-9);
    }

    [Fact]
    public void IntersectLineCircle_WhenLineIsTangent_ShouldReturnOnePoint()
    {
        var line = Line2D.FromPoints(
            new Point2D(-20, 10),
            new Point2D(20, 10));

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectLineCircle(line, circle);

        Assert.Single(points);
        Assert.Equal(0, points[0].X, precision: 10);
        Assert.Equal(10, points[0].Y, precision: 10);
    }

    [Fact]
    public void IntersectLineCircle_WhenLineDoesNotTouchCircle_ShouldReturnNoPoints()
    {
        var line = Line2D.FromPoints(
            new Point2D(-20, 20),
            new Point2D(20, 20));

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectLineCircle(line, circle);

        Assert.Empty(points);
    }

    [Fact]
    public void IntersectLineCircle_WhenLineDirectionIsZero_ShouldReturnNoPoints()
    {
        var line = new Line2D(
            new Point2D(0, 0),
            Vector2D.Zero);

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectLineCircle(line, circle);

        Assert.Empty(points);
    }

    [Fact]
    public void IntersectSegmentCircle_WhenSegmentCrossesCircle_ShouldReturnTwoPoints()
    {
        var segment = new LineSegment2D(
            new Point2D(-20, 0),
            new Point2D(20, 0));

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectSegmentCircle(segment, circle);

        Assert.Equal(2, points.Count);
    }

    [Fact]
    public void IntersectSegmentCircle_WhenSegmentEndsBeforeCircle_ShouldReturnNoPoints()
    {
        var segment = new LineSegment2D(
            new Point2D(-20, 0),
            new Point2D(-15, 0));

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectSegmentCircle(segment, circle);

        Assert.Empty(points);
    }

    [Fact]
    public void IntersectCircleCircle_WhenCirclesCross_ShouldReturnTwoPoints()
    {
        var first = new Circle2D(
            new Point2D(0, 0),
            10);

        var second = new Circle2D(
            new Point2D(10, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectCircleCircle(first, second);

        Assert.Equal(2, points.Count);

        Assert.Contains(points, point =>
            Math.Abs(point.X - 5) < 1e-9 &&
            Math.Abs(point.Y - Math.Sqrt(75)) < 1e-9);

        Assert.Contains(points, point =>
            Math.Abs(point.X - 5) < 1e-9 &&
            Math.Abs(point.Y + Math.Sqrt(75)) < 1e-9);
    }

    [Fact]
    public void IntersectCircleCircle_WhenCirclesAreTangent_ShouldReturnOnePoint()
    {
        var first = new Circle2D(
            new Point2D(0, 0),
            10);

        var second = new Circle2D(
            new Point2D(20, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectCircleCircle(first, second);

        Assert.Single(points);
        Assert.Equal(10, points[0].X, precision: 10);
        Assert.Equal(0, points[0].Y, precision: 10);
    }

    [Fact]
    public void IntersectArcCircle_WhenCircleIntersectsArc_ShouldReturnOnlyPointsOnArc()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        var circle = new Circle2D(
            new Point2D(10, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectArcCircle(arc, circle);

        Assert.Single(points);

        Point2D point = points[0];

        Assert.Equal(5, point.X, precision: 10);
        Assert.Equal(Math.Sqrt(75), point.Y, precision: 10);
        Assert.True(arc.ContainsPoint(point));
    }

    [Fact]
    public void IntersectArcCircle_WhenArcContainsBothIntersections_ShouldReturnTwoPoints()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(290),
            Angle.FromDegrees(70));

        var circle = new Circle2D(
            new Point2D(10, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectArcCircle(arc, circle);

        Assert.Equal(2, points.Count);

        Assert.Contains(points, point =>
            Math.Abs(point.X - 5) < 1e-9 &&
            Math.Abs(point.Y - Math.Sqrt(75)) < 1e-9);

        Assert.Contains(points, point =>
            Math.Abs(point.X - 5) < 1e-9 &&
            Math.Abs(point.Y + Math.Sqrt(75)) < 1e-9);

        Assert.All(points, point => Assert.True(arc.ContainsPoint(point)));
    }

    [Fact]
    public void IntersectArcCircle_WithClockwiseArcCrossingZero_ShouldReturnTangentPoint()
    {
        var arc = new Arc2D(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(10),
            Angle.FromDegrees(350),
            isCounterClockwise: false);

        var circle = new Circle2D(
            new Point2D(20, 0),
            10);

        IReadOnlyList<Point2D> points =
            CircleIntersectionService.IntersectArcCircle(arc, circle);

        Point2D point = Assert.Single(points);

        Assert.Equal(10, point.X, precision: 10);
        Assert.Equal(0, point.Y, precision: 10);
        Assert.True(arc.ContainsPoint(point));
    }
}