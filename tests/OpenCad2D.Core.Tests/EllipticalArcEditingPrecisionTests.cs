using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class EllipticalArcEditingPrecisionTests
{
    private const double Tolerance = 1.0e-6;

    [Fact]
    public void TrimEllipse_WithTwoLineBoundaries_ShouldKeepNativeEllipticalArcFragments()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var leftBoundary = new LineEntity(
            new Point2D(-5, -10),
            new Point2D(-5, 10));
        var rightBoundary = new LineEntity(
            new Point2D(5, -10),
            new Point2D(5, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            ellipse,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(0, 5));

        Assert.Equal(3, result.Count);
        Assert.All(result, entity => Assert.IsType<EllipticalArcEntity>(entity));
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
        Assert.All(result.OfType<EllipticalArcEntity>(), arc =>
        {
            Assert.Equal(ellipse.Center, arc.Center);
            Assert.Equal(ellipse.MajorAxis, arc.MajorAxis);
            Assert.Equal(ellipse.MinorRadius, arc.MinorRadius);
        });
    }

    [Fact]
    public void TrimEllipse_WithTwoLineBoundaries_ShouldUseGeometricCutEndpoints()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var leftBoundary = new LineEntity(
            new Point2D(-5, -10),
            new Point2D(-5, 10));
        var rightBoundary = new LineEntity(
            new Point2D(5, -10),
            new Point2D(5, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            ellipse,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(0, 5));

        var endpoints = result
            .OfType<EllipticalArcEntity>()
            .SelectMany(arc => new[] { arc.StartPoint, arc.EndPoint })
            .ToList();

        double expectedY = 5.0 * Math.Sqrt(1.0 - (5.0 * 5.0) / (10.0 * 10.0));
        AssertContainsPoint(endpoints, new Point2D(5, expectedY));
        AssertContainsPoint(endpoints, new Point2D(-5, expectedY));
        AssertContainsPoint(endpoints, new Point2D(-5, -expectedY));
        AssertContainsPoint(endpoints, new Point2D(5, -expectedY));

        Assert.All(endpoints, point => AssertPointOnEllipse(ellipse, point));
        Assert.All(endpoints, point => Assert.True(
            Math.Abs(Math.Abs(point.X) - 5.0) <= Tolerance,
            $"Expected endpoint {point} to lie on one of the vertical trim boundaries."));
    }

    [Fact]
    public void TrimEllipticalArc_ByLineBoundary_ShouldKeepNativeEndpointOnBoundaryAndEllipse()
    {
        var ellipticalArc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);
        var boundary = new LineEntity(
            new Point2D(0, -10),
            new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            ellipticalArc,
            boundary,
            new Point2D(5, 4));

        EllipticalArcEntity kept = Assert.IsType<EllipticalArcEntity>(Assert.Single(result));

        Assert.Equal(ellipticalArc.Center, kept.Center);
        Assert.Equal(ellipticalArc.MajorAxis, kept.MajorAxis);
        Assert.Equal(ellipticalArc.MinorRadius, kept.MinorRadius);
        Assert.True(
            kept.EndPoint.DistanceTo(new Point2D(0, 5)) <= Tolerance ||
            kept.StartPoint.DistanceTo(new Point2D(0, 5)) <= Tolerance);
        AssertPointOnEllipse(ellipticalArc, kept.StartPoint);
        AssertPointOnEllipse(ellipticalArc, kept.EndPoint);
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
    }

    [Fact]
    public void BreakEllipticalArc_AtPoint_ShouldCreateTwoNativeFragmentsSharingBreakPoint()
    {
        var ellipticalArc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);
        var breakPoint = new Point2D(0, 5);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            ellipticalArc,
            breakPoint);

        Assert.Equal(2, result.Count);
        var first = Assert.IsType<EllipticalArcEntity>(result[0]);
        var second = Assert.IsType<EllipticalArcEntity>(result[1]);

        Assert.True(first.EndPoint.DistanceTo(breakPoint) <= Tolerance);
        Assert.True(second.StartPoint.DistanceTo(breakPoint) <= Tolerance);
        AssertPointOnEllipse(ellipticalArc, first.StartPoint);
        AssertPointOnEllipse(ellipticalArc, first.EndPoint);
        AssertPointOnEllipse(ellipticalArc, second.StartPoint);
        AssertPointOnEllipse(ellipticalArc, second.EndPoint);
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
    }

    [Fact]
    public void BreakEllipticalArc_BetweenPoints_ShouldRemoveMiddleSegmentAndKeepNativeFragments()
    {
        var ellipticalArc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);
        Point2D firstBreakPoint = PointOnEllipse(ellipticalArc, Math.PI / 4.0);
        Point2D secondBreakPoint = PointOnEllipse(ellipticalArc, Math.PI * 3.0 / 4.0);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            ellipticalArc,
            firstBreakPoint,
            secondBreakPoint);

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.IsType<EllipticalArcEntity>(entity));
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);

        var fragments = result.OfType<EllipticalArcEntity>().ToList();
        Assert.True(fragments[0].EndPoint.DistanceTo(firstBreakPoint) <= Tolerance);
        Assert.True(fragments[1].StartPoint.DistanceTo(secondBreakPoint) <= Tolerance);
        Assert.All(fragments, fragment =>
        {
            Assert.Equal(ellipticalArc.Center, fragment.Center);
            Assert.Equal(ellipticalArc.MajorAxis, fragment.MajorAxis);
            Assert.Equal(ellipticalArc.MinorRadius, fragment.MinorRadius);
            AssertPointOnEllipse(ellipticalArc, fragment.StartPoint);
            AssertPointOnEllipse(ellipticalArc, fragment.EndPoint);
        });
    }


    [Fact]
    public void IntersectPolylineEllipse_ShouldReturnAnalyticEllipsePoints()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var boundary = new PolylineEntity(new[]
        {
            new Point2D(5, -10),
            new Point2D(5, 10)
        });

        IReadOnlyList<Point2D> points = CadEntityIntersectionService.Intersect(
            boundary,
            ellipse);

        double expectedY = 5.0 * Math.Sqrt(1.0 - (5.0 * 5.0) / (10.0 * 10.0));

        Assert.Equal(2, points.Count);
        AssertContainsPoint(points, new Point2D(5, expectedY));
        AssertContainsPoint(points, new Point2D(5, -expectedY));
        Assert.All(points, point =>
        {
            Assert.True(Math.Abs(point.X - 5.0) <= Tolerance);
            AssertPointOnEllipse(ellipse, point);
        });
    }

    [Fact]
    public void IntersectPolylineEllipticalArc_ShouldReturnOnlyPointsInsideArcSweep()
    {
        var ellipticalArc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);
        var boundary = new PolylineEntity(new[]
        {
            new Point2D(5, -10),
            new Point2D(5, 10)
        });

        IReadOnlyList<Point2D> points = CadEntityIntersectionService.Intersect(
            boundary,
            ellipticalArc);

        double expectedY = 5.0 * Math.Sqrt(1.0 - (5.0 * 5.0) / (10.0 * 10.0));

        Point2D point = Assert.Single(points);
        Assert.True(point.DistanceTo(new Point2D(5, expectedY)) <= Tolerance);
        AssertPointOnEllipse(ellipticalArc, point);
    }

    [Fact]
    public void TrimEllipse_ByPolylineBoundary_ShouldKeepNativeGeometricEndpoint()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var boundary = new PolylineEntity(new[]
        {
            new Point2D(5, -10),
            new Point2D(5, 10)
        });

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            ellipse,
            boundary,
            new Point2D(10, 0));

        EllipticalArcEntity kept = Assert.IsType<EllipticalArcEntity>(Assert.Single(result));
        double expectedY = 5.0 * Math.Sqrt(1.0 - (5.0 * 5.0) / (10.0 * 10.0));

        Assert.True(
            kept.StartPoint.DistanceTo(new Point2D(5, -expectedY)) <= Tolerance ||
            kept.EndPoint.DistanceTo(new Point2D(5, -expectedY)) <= Tolerance);
        Assert.True(
            kept.StartPoint.DistanceTo(new Point2D(5, expectedY)) <= Tolerance ||
            kept.EndPoint.DistanceTo(new Point2D(5, expectedY)) <= Tolerance);
        AssertPointOnEllipse(ellipse, kept.StartPoint);
        AssertPointOnEllipse(ellipse, kept.EndPoint);
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
    }

    [Fact]
    public void TrimEllipticalArc_ByPolylineBoundary_ShouldKeepNativeGeometricEndpoint()
    {
        var ellipticalArc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);
        var boundary = new PolylineEntity(new[]
        {
            new Point2D(5, -10),
            new Point2D(5, 10)
        });

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            ellipticalArc,
            boundary,
            new Point2D(8, 2));

        EllipticalArcEntity kept = Assert.IsType<EllipticalArcEntity>(Assert.Single(result));
        double expectedY = 5.0 * Math.Sqrt(1.0 - (5.0 * 5.0) / (10.0 * 10.0));

        Assert.True(
            kept.StartPoint.DistanceTo(new Point2D(5, expectedY)) <= Tolerance ||
            kept.EndPoint.DistanceTo(new Point2D(5, expectedY)) <= Tolerance);
        AssertPointOnEllipse(ellipticalArc, kept.StartPoint);
        AssertPointOnEllipse(ellipticalArc, kept.EndPoint);
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
    }

    [Fact]
    public void IntersectCircleEllipse_ShouldReturnPointsOnBothNativeCurves()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var circle = new CircleEntity(
            new Point2D(0, 0),
            8);

        IReadOnlyList<Point2D> points = CadEntityIntersectionService.Intersect(
            circle,
            ellipse);

        Assert.Equal(4, points.Count);
        Assert.All(points, point =>
        {
            AssertPointOnEllipse(ellipse, point);
            AssertPointOnCircle(circle, point);
        });
    }

    [Fact]
    public void TrimEllipse_ByCircleBoundary_ShouldKeepEndpointsOnCircleAndEllipse()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var circle = new CircleEntity(
            new Point2D(0, 0),
            8);

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            ellipse,
            circle,
            new Point2D(0, 5));

        Assert.All(result, entity => Assert.IsType<EllipticalArcEntity>(entity));
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);

        var endpoints = result
            .OfType<EllipticalArcEntity>()
            .SelectMany(arc => new[] { arc.StartPoint, arc.EndPoint })
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, point =>
        {
            AssertPointOnEllipse(ellipse, point);
            AssertPointOnCircle(circle, point);
        });
    }

    [Fact]
    public void TrimCircle_ByEllipseBoundary_ShouldKeepEndpointsOnCircleAndEllipse()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var circle = new CircleEntity(
            new Point2D(0, 0),
            8);

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            circle,
            ellipse,
            new Point2D(0, 8));

        Assert.All(result, entity => Assert.IsType<ArcEntity>(entity));

        var endpoints = result
            .OfType<ArcEntity>()
            .SelectMany(arc => new[] { arc.Geometry.StartPoint, arc.Geometry.EndPoint })
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, point =>
        {
            AssertPointOnCircle(circle, point);
            AssertPointOnEllipse(ellipse, point);
        });
    }

    private static Point2D PointOnEllipse(
        EllipticalArcEntity arc,
        double parameterRadians)
    {
        return arc.GetPointAt(parameterRadians);
    }

    private static void AssertContainsPoint(
        IEnumerable<Point2D> points,
        Point2D expected)
    {
        Assert.Contains(points, point => point.DistanceTo(expected) <= Tolerance);
    }

    private static void AssertPointOnCircle(
        CircleEntity circle,
        Point2D point)
    {
        Assert.True(
            Math.Abs(point.DistanceTo(circle.Center) - circle.Radius) <= Tolerance,
            $"Expected point {point} to lie on the source circle.");
    }

    private static void AssertPointOnEllipse(
        EllipseEntity ellipse,
        Point2D point)
    {
        AssertPointOnEllipse(
            ellipse.Center,
            ellipse.MajorDirection,
            ellipse.MajorRadius,
            ellipse.MinorAxis.Normalize(),
            ellipse.MinorRadius,
            point);
    }

    private static void AssertPointOnEllipse(
        EllipticalArcEntity arc,
        Point2D point)
    {
        AssertPointOnEllipse(
            arc.Center,
            arc.MajorDirection,
            arc.MajorRadius,
            arc.MinorAxis.Normalize(),
            arc.MinorRadius,
            point);
    }

    private static void AssertPointOnEllipse(
        Point2D center,
        Vector2D majorDirection,
        double majorRadius,
        Vector2D minorDirection,
        double minorRadius,
        Point2D point)
    {
        Vector2D fromCenter = center.VectorTo(point);
        double localX = fromCenter.Dot(majorDirection) / majorRadius;
        double localY = fromCenter.Dot(minorDirection) / minorRadius;
        double equation = localX * localX + localY * localY;

        Assert.True(
            Math.Abs(equation - 1.0) <= Tolerance,
            $"Expected point {point} to lie on the source ellipse; equation value was {equation}.");
    }
}
