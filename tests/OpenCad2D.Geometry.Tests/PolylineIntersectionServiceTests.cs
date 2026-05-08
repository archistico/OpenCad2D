using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class PolylineIntersectionServiceTests
{
    [Fact]
    public void IntersectSegmentPolyline_WhenSegmentCrossesPolyline_ShouldReturnIntersectionPoint()
    {
        var segment = new LineSegment2D(
            new Point2D(5, -5),
            new Point2D(5, 5));

        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        IReadOnlyList<Point2D> points =
            PolylineIntersectionService.IntersectSegmentPolyline(
                segment,
                polyline);

        Assert.Single(points);
        Assert.Equal(5, points[0].X, precision: 10);
        Assert.Equal(0, points[0].Y, precision: 10);
    }

    [Fact]
    public void IntersectSegmentPolyline_WhenSegmentDoesNotCrossPolyline_ShouldReturnNoPoints()
    {
        var segment = new LineSegment2D(
            new Point2D(0, 20),
            new Point2D(10, 20));

        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        IReadOnlyList<Point2D> points =
            PolylineIntersectionService.IntersectSegmentPolyline(
                segment,
                polyline);

        Assert.Empty(points);
    }

    [Fact]
    public void IntersectSegmentPolyline_WhenSegmentCrossesTwoSegments_ShouldReturnTwoPoints()
    {
        var segment = new LineSegment2D(
            new Point2D(5, -5),
            new Point2D(5, 15));

        var polyline = new Polyline2D(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true);

        IReadOnlyList<Point2D> points =
            PolylineIntersectionService.IntersectSegmentPolyline(
                segment,
                polyline);

        Assert.Equal(2, points.Count);

        Assert.Contains(points, point =>
            Math.Abs(point.X - 5) < 1e-9 &&
            Math.Abs(point.Y - 0) < 1e-9);

        Assert.Contains(points, point =>
            Math.Abs(point.X - 5) < 1e-9 &&
            Math.Abs(point.Y - 10) < 1e-9);
    }

    [Fact]
    public void IntersectPolylinePolyline_WhenPolylinesCross_ShouldReturnIntersectionPoint()
    {
        var first = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        var second = new Polyline2D(new[]
        {
            new Point2D(5, -5),
            new Point2D(5, 5)
        });

        IReadOnlyList<Point2D> points =
            PolylineIntersectionService.IntersectPolylinePolyline(
                first,
                second);

        Assert.Single(points);
        Assert.Equal(5, points[0].X, precision: 10);
        Assert.Equal(0, points[0].Y, precision: 10);
    }

    [Fact]
    public void IntersectPolylinePolyline_WhenPolylinesTouchAtSharedVertex_ShouldReturnOnePoint()
    {
        var first = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        var second = new Polyline2D(new[]
        {
            new Point2D(10, 10),
            new Point2D(20, 10)
        });

        IReadOnlyList<Point2D> points =
            PolylineIntersectionService.IntersectPolylinePolyline(
                first,
                second);

        Assert.Single(points);
        Assert.Equal(10, points[0].X, precision: 10);
        Assert.Equal(10, points[0].Y, precision: 10);
    }

    [Fact]
    public void IntersectPolylinePolyline_WhenPolylinesDoNotTouch_ShouldReturnNoPoints()
    {
        var first = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        var second = new Polyline2D(new[]
        {
            new Point2D(0, 10),
            new Point2D(10, 10)
        });

        IReadOnlyList<Point2D> points =
            PolylineIntersectionService.IntersectPolylinePolyline(
                first,
                second);

        Assert.Empty(points);
    }

    [Fact]
    public void IntersectsPolylinePolyline_WhenPolylinesCross_ShouldReturnTrue()
    {
        var first = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        var second = new Polyline2D(new[]
        {
            new Point2D(5, -5),
            new Point2D(5, 5)
        });

        bool result =
            PolylineIntersectionService.IntersectsPolylinePolyline(
                first,
                second);

        Assert.True(result);
    }

    [Fact]
    public void IntersectsPolylinePolyline_WhenPolylinesDoNotTouch_ShouldReturnFalse()
    {
        var first = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        var second = new Polyline2D(new[]
        {
            new Point2D(0, 10),
            new Point2D(10, 10)
        });

        bool result =
            PolylineIntersectionService.IntersectsPolylinePolyline(
                first,
                second);

        Assert.False(result);
    }
}