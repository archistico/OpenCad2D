using OpenCad2D.Core.Editing.Curves;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadCurveSplitServiceTests
{
    [Fact]
    public void SplitAtPoint_WithLine_ShouldCreateTwoLineFragmentsSharingProjectedPoint()
    {
        var service = new CadCurveSplitService();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));

        IReadOnlyList<CadEntity> result = service.SplitAtPoint(
            line,
            new Point2D(5, 2));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<LineEntity>(result[0]);
        var second = Assert.IsType<LineEntity>(result[1]);

        Assert.Equal(new Point2D(5, 0), first.End);
        Assert.Equal(first.End, second.Start);
    }

    [Fact]
    public void RemovePickedInterval_WithCircleCuts_ShouldCreateNativeArcFragments()
    {
        var service = new CadCurveSplitService();
        var circle = new CircleEntity(new Point2D(0, 0), 10);

        IReadOnlyList<CadEntity> result = service.RemovePickedInterval(
            circle,
            new[]
            {
                new Point2D(10, 0),
                new Point2D(0, 10),
                new Point2D(-10, 0)
            },
            new Point2D(0, 10));

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.IsType<ArcEntity>(entity));
    }

    [Fact]
    public void RemovePickedInterval_WithArcCuts_ShouldCreateNativeArcFragments()
    {
        var service = new CadCurveSplitService();
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        IReadOnlyList<CadEntity> result = service.RemovePickedInterval(
            arc,
            new[]
            {
                PointOnCircle(10, 45),
                PointOnCircle(10, 135)
            },
            PointOnCircle(10, 90));

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.IsType<ArcEntity>(entity));
    }


    [Fact]
    public void SplitAtPoint_WithOpenPolyline_ShouldCreateTwoPolylineFragmentsSharingProjectedVertex()
    {
        var service = new CadCurveSplitService();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        IReadOnlyList<CadEntity> result = service.SplitAtPoint(
            polyline,
            new Point2D(10, 5));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<PolylineEntity>(result[0]);
        var second = Assert.IsType<PolylineEntity>(result[1]);

        Assert.Equal(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5)
            },
            first.Vertices);
        Assert.Equal(
            new[]
            {
                new Point2D(10, 5),
                new Point2D(10, 10)
            },
            second.Vertices);
        Assert.Equal(first.Vertices[^1], second.Vertices[0]);
    }

    [Fact]
    public void RemoveBetweenPoints_WithOpenPolyline_ShouldRemoveMiddlePathAndPreserveNativeVertices()
    {
        var service = new CadCurveSplitService();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        IReadOnlyList<CadEntity> result = service.RemoveBetweenPoints(
            polyline,
            new Point2D(5, 0),
            new Point2D(10, 5));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<PolylineEntity>(result[0]);
        var second = Assert.IsType<PolylineEntity>(result[1]);

        Assert.Equal(
            new[] { new Point2D(0, 0), new Point2D(5, 0) },
            first.Vertices);
        Assert.Equal(
            new[] { new Point2D(10, 5), new Point2D(10, 10) },
            second.Vertices);
    }

    [Fact]
    public void RemoveBetweenPoints_WithClosedPolylinePolygon_ShouldReturnOpenPolylineAroundRemainingPath()
    {
        var service = new CadCurveSplitService();
        var polygon = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        }, isClosed: true);

        IReadOnlyList<CadEntity> result = service.RemoveBetweenPoints(
            polygon,
            new Point2D(5, 0),
            new Point2D(10, 5));

        PolylineEntity remaining = Assert.IsType<PolylineEntity>(Assert.Single(result));

        Assert.False(remaining.IsClosed);
        Assert.Equal(
            new[]
            {
                new Point2D(10, 5),
                new Point2D(10, 10),
                new Point2D(0, 10),
                new Point2D(0, 0),
                new Point2D(5, 0)
            },
            remaining.Vertices);
    }


    [Fact]
    public void SplitAtPoint_WithBezierSpline_ShouldCreateTwoNativeBezierFragmentsSharingBreakPoint()
    {
        var service = new CadCurveSplitService();
        var spline = CreateCubicSpline();
        Point2D breakPoint = BezierSplineSplitService.Evaluate(spline, 0.5);

        IReadOnlyList<CadEntity> result = service.SplitAtPoint(
            spline,
            breakPoint);

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<BezierSplineEntity>(result[0]);
        var second = Assert.IsType<BezierSplineEntity>(result[1]);

        AssertPointNear(breakPoint, first.ControlPoints[^1]);
        Assert.Equal(first.ControlPoints[^1], second.ControlPoints[0]);
        Assert.False(first.IsClosed);
        Assert.False(second.IsClosed);
    }

    [Fact]
    public void RemoveBetweenPoints_WithBezierSpline_ShouldReturnNativeOuterFragments()
    {
        var service = new CadCurveSplitService();
        var spline = CreateCubicSpline();

        IReadOnlyList<CadEntity> result = service.RemoveBetweenPoints(
            spline,
            BezierSplineSplitService.Evaluate(spline, 0.25),
            BezierSplineSplitService.Evaluate(spline, 0.75));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<BezierSplineEntity>(result[0]);
        var second = Assert.IsType<BezierSplineEntity>(result[1]);

        AssertPointNear(
            BezierSplineSplitService.Evaluate(spline, 0.25),
            first.ControlPoints[^1]);
        AssertPointNear(
            BezierSplineSplitService.Evaluate(spline, 0.75),
            second.ControlPoints[0]);
        Assert.Equal(spline.ControlPoints[0], first.ControlPoints[0]);
        Assert.Equal(spline.ControlPoints[^1], second.ControlPoints[^1]);
    }

    [Fact]
    public void RemovePickedInterval_WithBezierSplineCuts_ShouldReturnNativeBezierFragments()
    {
        var service = new CadCurveSplitService();
        var spline = CreateCubicSpline();

        IReadOnlyList<CadEntity> result = service.RemovePickedInterval(
            spline,
            new[]
            {
                new CurveCut(0.25, BezierSplineSplitService.Evaluate(spline, 0.25)),
                new CurveCut(0.75, BezierSplineSplitService.Evaluate(spline, 0.75))
            },
            BezierSplineSplitService.Evaluate(spline, 0.5));

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.IsType<BezierSplineEntity>(entity));
    }

    [Fact]
    public void SplitAtPoint_WithClosedBezierSpline_ShouldReturnNoFragmentsForNow()
    {
        var service = new CadCurveSplitService();
        var spline = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            },
            isClosed: true);

        IReadOnlyList<CadEntity> result = service.SplitAtPoint(
            spline,
            new Point2D(5, 5));

        Assert.Empty(result);
    }


    private static void AssertPointNear(
        Point2D expected,
        Point2D actual)
    {
        Assert.True(
            expected.DistanceTo(actual) < 1e-8,
            $"Expected point {actual} to be near {expected}.");
    }

    private static BezierSplineEntity CreateCubicSpline()
    {
        return new BezierSplineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(3, 9),
            new Point2D(7, -9),
            new Point2D(10, 0)
        });
    }

    private static Point2D PointOnCircle(
        double radius,
        double degrees)
    {
        double radians = degrees * Math.PI / 180.0;

        return new Point2D(
            Math.Cos(radians) * radius,
            Math.Sin(radians) * radius);
    }
}
