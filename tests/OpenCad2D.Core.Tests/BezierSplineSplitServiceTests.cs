using OpenCad2D.Core.Editing.Curves;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class BezierSplineSplitServiceTests
{
    private static readonly BezierSplineSplitService SplitService = new();

    [Fact]
    public void SplitAt_WithQuadraticBezier_ShouldCreateTwoNativeBezierSplinesSharingBreakPoint()
    {
        var spline = CreateQuadraticSpline();

        IReadOnlyList<BezierSplineEntity> fragments = SplitService.SplitAt(spline, 0.5);

        Assert.Equal(2, fragments.Count);
        BezierSplineEntity left = fragments[0];
        BezierSplineEntity right = fragments[1];

        Assert.False(left.IsClosed);
        Assert.False(right.IsClosed);
        Assert.Equal(3, left.ControlPoints.Count);
        Assert.Equal(3, right.ControlPoints.Count);

        Point2D expectedBreakPoint = BezierSplineSplitService.Evaluate(spline, 0.5);
        Assert.Equal(expectedBreakPoint, left.ControlPoints[^1]);
        Assert.Equal(expectedBreakPoint, right.ControlPoints[0]);
        Assert.Equal(left.ControlPoints[^1], right.ControlPoints[0]);
    }

    [Fact]
    public void SplitAt_ShouldPreserveLayerStyleAndVisibilityMetadata()
    {
        var layerId = new LayerId("SplineLayer");
        var spline = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(4, 8),
                new Point2D(10, 0)
            },
            layerId: layerId,
            style: new EntityStyle
            {
                Color = CadColor.FromRgb(10, 20, 30),
                LineWeight = LineWeight.FromMillimeters(0.5)
            },
            isVisible: false,
            isLocked: true,
            drawOrder: 42);

        IReadOnlyList<BezierSplineEntity> fragments = SplitService.SplitAt(spline, 0.25);

        Assert.All(fragments, fragment =>
        {
            Assert.Equal(layerId, fragment.LayerId);
            Assert.Equal(spline.Style, fragment.Style);
            Assert.False(fragment.IsVisible);
            Assert.True(fragment.IsLocked);
            Assert.Equal(42, fragment.DrawOrder);
        });
    }

    [Fact]
    public void ExtractInterval_ShouldCreateNativeBezierSplineWithExpectedEndpoints()
    {
        var spline = CreateCubicSpline();

        BezierSplineEntity? interval = SplitService.ExtractInterval(spline, 0.25, 0.75);

        Assert.NotNull(interval);
        Assert.Equal(spline.ControlPoints.Count, interval.ControlPoints.Count);
        AssertPointNear(
            BezierSplineSplitService.Evaluate(spline, 0.25),
            interval.ControlPoints[0]);
        AssertPointNear(
            BezierSplineSplitService.Evaluate(spline, 0.75),
            interval.ControlPoints[^1]);
    }

    [Fact]
    public void RemoveInterval_ShouldReturnNativeOuterBezierFragments()
    {
        var spline = CreateCubicSpline();

        IReadOnlyList<BezierSplineEntity> fragments = SplitService.RemoveInterval(spline, 0.25, 0.75);

        Assert.Equal(2, fragments.Count);
        Assert.Equal(
            spline.ControlPoints[0],
            fragments[0].ControlPoints[0]);
        AssertPointNear(
            BezierSplineSplitService.Evaluate(spline, 0.25),
            fragments[0].ControlPoints[^1]);
        AssertPointNear(
            BezierSplineSplitService.Evaluate(spline, 0.75),
            fragments[1].ControlPoints[0]);
        Assert.Equal(
            spline.ControlPoints[^1],
            fragments[1].ControlPoints[^1]);
    }

    [Fact]
    public void SplitAt_WithEndpointParameter_ShouldReturnNoFragments()
    {
        var spline = CreateQuadraticSpline();

        Assert.Empty(SplitService.SplitAt(spline, 0.0));
        Assert.Empty(SplitService.SplitAt(spline, 1.0));
    }

    [Fact]
    public void SplitAt_WithClosedSpline_ShouldReturnNoFragmentsForNow()
    {
        var spline = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            },
            isClosed: true);

        Assert.Empty(SplitService.SplitAt(spline, 0.5));
    }

    private static void AssertPointNear(Point2D expected, Point2D actual)
    {
        Assert.True(
            expected.DistanceTo(actual) < 1e-9,
            $"Expected point {actual} to be near {expected}.");
    }

    private static BezierSplineEntity CreateQuadraticSpline()
    {
        return new BezierSplineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 10),
            new Point2D(10, 0)
        });
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
}
