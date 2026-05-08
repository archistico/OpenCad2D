using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class RectangleIntersectionServiceTests
{
    [Fact]
    public void IntersectsSegment_WhenSegmentCrossesRectangle_ShouldReturnTrue()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var segment = new LineSegment2D(
            new Point2D(-5, 5),
            new Point2D(5, 5));

        bool result = RectangleIntersectionService.IntersectsSegment(
            rectangle,
            segment);

        Assert.True(result);
    }

    [Fact]
    public void IntersectsSegment_WhenSegmentIsOutsideRectangle_ShouldReturnFalse()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var segment = new LineSegment2D(
            new Point2D(-5, 20),
            new Point2D(5, 20));

        bool result = RectangleIntersectionService.IntersectsSegment(
            rectangle,
            segment);

        Assert.False(result);
    }

    [Fact]
    public void IntersectsPolyline_WhenPolylineCrossesRectangle_ShouldReturnTrue()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var polyline = new Polyline2D(new[]
        {
            new Point2D(-5, 5),
            new Point2D(5, 5),
            new Point2D(5, 15)
        });

        bool result = RectangleIntersectionService.IntersectsPolyline(
            rectangle,
            polyline);

        Assert.True(result);
    }

    [Fact]
    public void IntersectsPolyline_WhenPolylineIsOutsideRectangle_ShouldReturnFalse()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var polyline = new Polyline2D(new[]
        {
            new Point2D(20, 20),
            new Point2D(30, 20)
        });

        bool result = RectangleIntersectionService.IntersectsPolyline(
            rectangle,
            polyline);

        Assert.False(result);
    }

    [Fact]
    public void IntersectsCircle_WhenCircleCrossesRectangle_ShouldReturnTrue()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var circle = new Circle2D(
            new Point2D(12, 5),
            3);

        bool result = RectangleIntersectionService.IntersectsCircle(
            rectangle,
            circle);

        Assert.True(result);
    }

    [Fact]
    public void IntersectsCircle_WhenCircleIsOutsideRectangle_ShouldReturnFalse()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var circle = new Circle2D(
            new Point2D(20, 5),
            3);

        bool result = RectangleIntersectionService.IntersectsCircle(
            rectangle,
            circle);

        Assert.False(result);
    }

    [Fact]
    public void IntersectsArc_WhenArcCrossesRectangle_ShouldReturnTrue()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var arc = new Arc2D(
            new Point2D(5, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        bool result = RectangleIntersectionService.IntersectsArc(
            rectangle,
            arc);

        Assert.True(result);
    }

    [Fact]
    public void IntersectsArc_WhenArcBoundingBoxIntersectsButArcDoesNot_ShouldReturnFalse()
    {
        var rectangle = new BoundingBox2D(0, 0, 10, 10);

        var arc = new Arc2D(
            new Point2D(5, -20),
            25,
            Angle.FromDegrees(200),
            Angle.FromDegrees(340));

        bool result = RectangleIntersectionService.IntersectsArc(
            rectangle,
            arc);

        Assert.False(result);
    }
}