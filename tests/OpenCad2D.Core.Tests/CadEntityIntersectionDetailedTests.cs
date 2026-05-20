using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadEntityIntersectionDetailedTests
{
    [Fact]
    public void IntersectDetailed_WithTwoLines_ShouldReturnSharedPointAndNativeParameters()
    {
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));

        CadIntersectionPoint intersection = Assert.Single(
            CadEntityIntersectionService.IntersectDetailed(horizontal, vertical));

        Assert.Equal(new Point2D(5, 0), intersection.Point);
        Assert.Equal(0.5, intersection.FirstParameter, 12);
        Assert.Equal(0.5, intersection.SecondParameter, 12);
        Assert.Equal(CadIntersectionKind.Crossing, intersection.Kind);
        Assert.Equal(intersection.Point, intersection.FirstCut.Point);
        Assert.Equal(intersection.Point, intersection.SecondCut.Point);
    }

    [Fact]
    public void IntersectDetailed_WhenLineTouchesOtherLineEndpoint_ShouldClassifyEndpoint()
    {
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5));

        CadIntersectionPoint intersection = Assert.Single(
            CadEntityIntersectionService.IntersectDetailed(first, second));

        Assert.Equal(new Point2D(10, 0), intersection.Point);
        Assert.Equal(1.0, intersection.FirstParameter, 12);
        Assert.Equal(0.0, intersection.SecondParameter, 12);
        Assert.Equal(CadIntersectionKind.Endpoint, intersection.Kind);
    }

    [Fact]
    public void IntersectDetailed_WithLineAndCircle_ShouldReturnSharedPointsAndCircleParameters()
    {
        var line = new LineEntity(new Point2D(-20, 0), new Point2D(20, 0));
        var circle = new CircleEntity(new Point2D(0, 0), 10);

        IReadOnlyList<CadIntersectionPoint> intersections = CadEntityIntersectionService.IntersectDetailed(
            line,
            circle);

        Assert.Equal(2, intersections.Count);
        Assert.Contains(intersections, intersection =>
            intersection.Point == new Point2D(-10, 0) &&
            Math.Abs(intersection.FirstParameter - 0.25) < 1e-12 &&
            Math.Abs(intersection.SecondParameter - Math.PI) < 1e-12);
        Assert.Contains(intersections, intersection =>
            intersection.Point == new Point2D(10, 0) &&
            Math.Abs(intersection.FirstParameter - 0.75) < 1e-12 &&
            Math.Abs(intersection.SecondParameter) < 1e-12);
    }

    [Fact]
    public void IntersectDetailed_WithCircleAndEllipse_ShouldReturnSameSharedPointForBothCuts()
    {
        var circle = new CircleEntity(new Point2D(0, 0), 5);
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(8, 0),
            3);

        IReadOnlyList<CadIntersectionPoint> intersections = CadEntityIntersectionService.IntersectDetailed(
            circle,
            ellipse);

        Assert.Equal(4, intersections.Count);
        Assert.All(intersections, intersection =>
        {
            Assert.Equal(intersection.Point, intersection.FirstCut.Point);
            Assert.Equal(intersection.Point, intersection.SecondCut.Point);
            Assert.True(Math.Abs(intersection.Point.DistanceTo(circle.Center) - circle.Radius) < 1e-7);
            Assert.True(IsOnEllipse(intersection.Point, ellipse, 1e-7));
        });
    }

    private static bool IsOnEllipse(
        Point2D point,
        EllipseEntity ellipse,
        double tolerance)
    {
        Vector2D local = ellipse.Center.VectorTo(point);
        double x = local.Dot(ellipse.MajorDirection) / ellipse.MajorRadius;
        double y = local.Dot(ellipse.MinorAxis.Normalize()) / ellipse.MinorRadius;

        return Math.Abs((x * x) + (y * y) - 1.0) <= tolerance;
    }
}
