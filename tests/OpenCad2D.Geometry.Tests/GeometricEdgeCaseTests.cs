using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

/// <summary>
/// Covers geometric edge cases that are especially important for CAD editing tools:
/// tangencies, overlapping entities, shared vertices and tolerance-bound intersections.
/// </summary>
public sealed class GeometricEdgeCaseTests
{
    [Fact]
    public void IntersectSegments_WhenIntersectionIsJustBeyondEndpointWithinTolerance_ShouldReturnPoint()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(10.0000000005, -5),
            new Point2D(10.0000000005, 5));

        IntersectionResult result = IntersectionService.IntersectSegments(
            first,
            second,
            tolerance: 1e-9);

        Assert.Equal(IntersectionKind.Point, result.Kind);
        Assert.NotNull(result.Point);
        Assert.Equal(10.0000000005, result.Point.Value.X, precision: 10);
        Assert.Equal(0, result.Point.Value.Y, precision: 10);
    }

    [Fact]
    public void IntersectSegments_WhenIntersectionIsJustBeyondEndpointOutsideTolerance_ShouldReturnNone()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(10.000001, -5),
            new Point2D(10.000001, 5));

        IntersectionResult result = IntersectionService.IntersectSegments(
            first,
            second,
            tolerance: 1e-9);

        Assert.Equal(IntersectionKind.None, result.Kind);
    }

    [Fact]
    public void IntersectSegments_WhenCollinearSegmentsShareOnlyOneEndpoint_ShouldReturnSinglePoint()
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
    public void IntersectSegments_WhenCollinearSegmentsPartiallyOverlap_ShouldReturnOverlapping()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(2.5, 0),
            new Point2D(7.5, 0));

        IntersectionResult result = IntersectionService.IntersectSegments(first, second);

        Assert.Equal(IntersectionKind.Overlapping, result.Kind);
    }

    [Fact]
    public void IntersectSegments_WhenNearlyCollinearInsideTolerance_ShouldReturnOverlapping()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(2, 0.00000000005),
            new Point2D(8, 0.00000000005));

        IntersectionResult result = IntersectionService.IntersectSegments(
            first,
            second,
            tolerance: 1e-9);

        Assert.Equal(IntersectionKind.Overlapping, result.Kind);
    }

    [Fact]
    public void IntersectSegments_WhenNearlyCollinearOutsideTolerance_ShouldReturnNone()
    {
        var first = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineSegment2D(
            new Point2D(2, 0.0000000002),
            new Point2D(8, 0.0000000002));

        IntersectionResult result = IntersectionService.IntersectSegments(
            first,
            second,
            tolerance: 1e-9);

        Assert.Equal(IntersectionKind.None, result.Kind);
    }

    [Fact]
    public void IntersectLineCircle_WhenLineIsAlmostTangentInsideTolerance_ShouldReturnOnePoint()
    {
        var line = Line2D.FromPoints(
            new Point2D(-20, 10.0000000005),
            new Point2D(20, 10.0000000005));

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points = CircleIntersectionService.IntersectLineCircle(
            line,
            circle,
            tolerance: 1e-9);

        Assert.Single(points);
        Assert.Equal(0, points[0].X, precision: 10);
        Assert.Equal(10.0000000005, points[0].Y, precision: 10);
    }

    [Fact]
    public void IntersectLineCircle_WhenLineIsAlmostTangentOutsideTolerance_ShouldReturnNoPoints()
    {
        var line = Line2D.FromPoints(
            new Point2D(-20, 10.000001),
            new Point2D(20, 10.000001));

        var circle = new Circle2D(
            new Point2D(0, 0),
            10);

        IReadOnlyList<Point2D> points = CircleIntersectionService.IntersectLineCircle(
            line,
            circle,
            tolerance: 1e-9);

        Assert.Empty(points);
    }

    [Fact]
    public void IntersectCircleCircle_WhenCirclesAreAlmostExternallyTangentInsideTolerance_ShouldReturnOnePoint()
    {
        var first = new Circle2D(
            new Point2D(0, 0),
            10);

        var second = new Circle2D(
            new Point2D(20.00000000005, 0),
            10);

        IReadOnlyList<Point2D> points = CircleIntersectionService.IntersectCircleCircle(
            first,
            second,
            tolerance: 1e-9);

        Assert.Single(points);
        Assert.Equal(10.000000000025, points[0].X, precision: 10);
        Assert.Equal(0, points[0].Y, precision: 10);
    }

    [Fact]
    public void IntersectSegmentPolyline_WhenSegmentCrossesPolylineAtSharedVertex_ShouldReturnSingleDistinctPoint()
    {
        var segment = new LineSegment2D(
            new Point2D(10, -5),
            new Point2D(10, 5));

        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(20, 0)
        });

        IReadOnlyList<Point2D> points = PolylineIntersectionService.IntersectSegmentPolyline(
            segment,
            polyline);

        Assert.Single(points);
        Assert.Equal(new Point2D(10, 0), points[0]);
    }

    [Fact]
    public void IntersectPolylinePolyline_WhenClosedPolylinesShareACorner_ShouldReturnSingleDistinctPoint()
    {
        var first = new Polyline2D(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true);

        var second = new Polyline2D(
            new[]
            {
                new Point2D(10, 10),
                new Point2D(20, 10),
                new Point2D(20, 20),
                new Point2D(10, 20)
            },
            isClosed: true);

        IReadOnlyList<Point2D> points = PolylineIntersectionService.IntersectPolylinePolyline(
            first,
            second);

        Assert.Single(points);
        Assert.Equal(new Point2D(10, 10), points[0]);
    }
}
