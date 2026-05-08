using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class IntersectionServiceTests
{
    [Fact]
    public void IntersectSegments_WhenSegmentsCross_ShouldReturnIntersectionPoint()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 10));

        var second = new LineSegment2D(
            new Point2D(0, 10),
            new Point2D(10, 0));

        IntersectionResult result = IntersectionService.IntersectSegments(first, second);

        Assert.Equal(IntersectionKind.Point, result.Kind);
        Assert.NotNull(result.Point);
        Assert.Equal(5, result.Point.Value.X, precision: 10);
        Assert.Equal(5, result.Point.Value.Y, precision: 10);
    }

    [Fact]
    public void IntersectSegments_WhenSegmentsDoNotTouch_ShouldReturnNone()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(0, 5),
            new Point2D(10, 5));

        IntersectionResult result = IntersectionService.IntersectSegments(first, second);

        Assert.Equal(IntersectionKind.None, result.Kind);
    }

    [Fact]
    public void IntersectSegments_WhenSegmentsTouchAtEndpoint_ShouldReturnPoint()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(10, 0),
            new Point2D(20, 0));

        IntersectionResult result = IntersectionService.IntersectSegments(first, second);

        Assert.Equal(IntersectionKind.Point, result.Kind);
        Assert.NotNull(result.Point);
        Assert.Equal(new Point2D(10, 0), result.Point.Value);
    }

    [Fact]
    public void IntersectSegments_WhenSegmentsOverlap_ShouldReturnOverlapping()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(5, 0),
            new Point2D(15, 0));

        IntersectionResult result = IntersectionService.IntersectSegments(first, second);

        Assert.Equal(IntersectionKind.Overlapping, result.Kind);
    }

    [Fact]
    public void IntersectSegments_WhenSegmentsAreCollinearButSeparated_ShouldReturnNone()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(20, 0),
            new Point2D(30, 0));

        IntersectionResult result = IntersectionService.IntersectSegments(first, second);

        Assert.Equal(IntersectionKind.None, result.Kind);
    }

    [Fact]
    public void IntersectSegments_WhenVerticalAndHorizontalSegmentsCross_ShouldReturnPoint()
    {
        var first = new LineSegment2D(
            new Point2D(5, -5),
            new Point2D(5, 5));

        var second = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        IntersectionResult result = IntersectionService.IntersectSegments(first, second);

        Assert.Equal(IntersectionKind.Point, result.Kind);
        Assert.NotNull(result.Point);
        Assert.Equal(new Point2D(5, 0), result.Point.Value);
    }
}