using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadBreakServiceTests
{
    [Fact]
    public void BreakAtPoint_WithArc_ShouldCreateTwoArcs()
    {
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            arc,
            new Point2D(0, 10));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<ArcEntity>(result[0]);
        var second = Assert.IsType<ArcEntity>(result[1]);

        Assert.Equal(0, first.StartAngle.Degrees, precision: 10);
        Assert.Equal(90, first.EndAngle.Degrees, precision: 10);
        Assert.Equal(90, second.StartAngle.Degrees, precision: 10);
        Assert.Equal(180, second.EndAngle.Degrees, precision: 10);
        Assert.True(first.IsCounterClockwise);
        Assert.True(second.IsCounterClockwise);
    }

    [Fact]
    public void BreakAtPoint_WithClockwiseArc_ShouldCreateTwoClockwiseArcs()
    {
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(180),
            Angle.FromDegrees(0),
            isCounterClockwise: false);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            arc,
            new Point2D(0, 10));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<ArcEntity>(result[0]);
        var second = Assert.IsType<ArcEntity>(result[1]);

        Assert.False(first.IsCounterClockwise);
        Assert.False(second.IsCounterClockwise);
        Assert.Equal(180, first.StartAngle.Degrees, precision: 10);
        Assert.Equal(90, first.EndAngle.Degrees, precision: 10);
        Assert.Equal(90, second.StartAngle.Degrees, precision: 10);
        Assert.Equal(0, second.EndAngle.Degrees, precision: 10);
    }

    [Fact]
    public void BreakAtPoint_WithArcEndpoint_ShouldReturnNoSegments()
    {
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            arc,
            new Point2D(10, 0));

        Assert.Empty(result);
    }

    [Fact]
    public void BreakAtPoint_WithOpenPolyline_ShouldCreateTwoOpenPolylines()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            });

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            polyline,
            new Point2D(5, 0));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<PolylineEntity>(result[0]);
        var second = Assert.IsType<PolylineEntity>(result[1]);

        Assert.False(first.IsClosed);
        Assert.False(second.IsClosed);
        Assert.Equal(
            new[] { new Point2D(0, 0), new Point2D(5, 0) },
            first.Vertices);
        Assert.Equal(
            new[] { new Point2D(5, 0), new Point2D(10, 0), new Point2D(10, 10) },
            second.Vertices);
    }

    [Fact]
    public void BreakAtPoint_WithOpenPolylineInternalVertex_ShouldCreateTwoOpenPolylines()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            });

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            polyline,
            new Point2D(10, 0));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<PolylineEntity>(result[0]);
        var second = Assert.IsType<PolylineEntity>(result[1]);

        Assert.Equal(
            new[] { new Point2D(0, 0), new Point2D(10, 0) },
            first.Vertices);
        Assert.Equal(
            new[] { new Point2D(10, 0), new Point2D(10, 10) },
            second.Vertices);
    }

    [Fact]
    public void BreakAtPoint_WithOpenPolylineEndpoint_ShouldReturnNoSegments()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            });

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            polyline,
            new Point2D(0, 0));

        Assert.Empty(result);
    }

    [Fact]
    public void BreakAtPoint_WithClosedPolyline_ShouldOpenPolylineAtBreakPoint()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            polyline,
            new Point2D(5, 0));

        PolylineEntity opened = Assert.IsType<PolylineEntity>(Assert.Single(result));

        Assert.False(opened.IsClosed);
        Assert.Equal(new Point2D(5, 0), opened.Vertices.First());
        Assert.Equal(new Point2D(5, 0), opened.Vertices.Last());
        Assert.Contains(new Point2D(10, 0), opened.Vertices);
        Assert.Contains(new Point2D(0, 10), opened.Vertices);
    }

    [Fact]
    public void BreakAtPoint_WithCircle_ShouldReturnNoSegments()
    {
        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            circle,
            new Point2D(10, 0));

        Assert.Empty(result);
    }

    [Fact]
    public void BreakBetweenPoints_WithArc_ShouldRemoveArcPortionBetweenPoints()
    {
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            arc,
            PointOnCircle(10, 30),
            PointOnCircle(10, 120));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<ArcEntity>(result[0]);
        var second = Assert.IsType<ArcEntity>(result[1]);

        Assert.Equal(0, first.StartAngle.Degrees, precision: 10);
        Assert.Equal(30, first.EndAngle.Degrees, precision: 10);
        Assert.Equal(120, second.StartAngle.Degrees, precision: 10);
        Assert.Equal(180, second.EndAngle.Degrees, precision: 10);
    }

    [Fact]
    public void BreakBetweenPoints_WithCircle_ShouldRemoveMinorArcAndReturnRemainingArc()
    {
        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            circle,
            new Point2D(10, 0),
            new Point2D(0, 10));

        ArcEntity remainingArc = Assert.IsType<ArcEntity>(Assert.Single(result));

        Assert.Equal(90, remainingArc.StartAngle.Degrees, precision: 10);
        Assert.Equal(0, remainingArc.EndAngle.Degrees, precision: 10);
        Assert.True(remainingArc.IsCounterClockwise);
    }

    [Fact]
    public void BreakBetweenPoints_WithOpenPolyline_ShouldRemovePolylineSegmentBetweenPoints()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            });

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            polyline,
            new Point2D(5, 0),
            new Point2D(10, 5));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<PolylineEntity>(result[0]);
        var second = Assert.IsType<PolylineEntity>(result[1]);

        Assert.False(first.IsClosed);
        Assert.False(second.IsClosed);
        Assert.Equal(
            new[] { new Point2D(0, 0), new Point2D(5, 0) },
            first.Vertices);
        Assert.Equal(
            new[] { new Point2D(10, 5), new Point2D(10, 10) },
            second.Vertices);
    }

    [Fact]
    public void BreakBetweenPoints_WithClosedPolyline_ShouldRemoveShortestPathAndReturnOpenPolyline()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            polyline,
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
    public void BreakBetweenPoints_WithOpenPolylinePointsOnSameSegment_ShouldRemoveMiddleSegment()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            });

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            polyline,
            new Point2D(2, 0),
            new Point2D(7, 0));

        Assert.Equal(2, result.Count);

        var first = Assert.IsType<PolylineEntity>(result[0]);
        var second = Assert.IsType<PolylineEntity>(result[1]);

        Assert.Equal(
            new[] { new Point2D(0, 0), new Point2D(2, 0) },
            first.Vertices);
        Assert.Equal(
            new[] { new Point2D(7, 0), new Point2D(10, 0), new Point2D(10, 10) },
            second.Vertices);
    }

    [Fact]
    public void BreakBetweenPoints_WithClosedPolylinePolygonPointsOnSameSegment_ShouldRemoveShortestPath()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            polyline,
            new Point2D(2, 0),
            new Point2D(7, 0));

        PolylineEntity remaining = Assert.IsType<PolylineEntity>(Assert.Single(result));

        Assert.False(remaining.IsClosed);
        Assert.Equal(new Point2D(7, 0), remaining.Vertices.First());
        Assert.Equal(new Point2D(2, 0), remaining.Vertices.Last());
        Assert.Contains(new Point2D(10, 10), remaining.Vertices);
        Assert.Contains(new Point2D(0, 10), remaining.Vertices);
    }

    [Fact]
    public void BreakAtPoint_WithEllipse_ShouldOpenEllipseAsPolyline()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakAtPoint(
            ellipse,
            new Point2D(10, 0));

        PolylineEntity opened = Assert.IsType<PolylineEntity>(Assert.Single(result));

        Assert.False(opened.IsClosed);
        Assert.True(opened.Vertices.Count > 8);
        Assert.True(opened.Vertices.First().DistanceTo(new Point2D(10, 0)) <= 1.0e-6);
        Assert.True(opened.Vertices.Last().DistanceTo(new Point2D(10, 0)) <= 1.0e-6);
    }

    [Fact]
    public void BreakBetweenPoints_WithEllipse_ShouldRemoveMinorArcAndReturnPolyline()
    {
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            ellipse,
            new Point2D(10, 0),
            new Point2D(0, 5));

        PolylineEntity remaining = Assert.IsType<PolylineEntity>(Assert.Single(result));

        Assert.False(remaining.IsClosed);
        Assert.True(remaining.Vertices.Count > 8);
        Assert.True(remaining.Vertices.First().DistanceTo(new Point2D(0, 5)) <= 1.0e-6);
        Assert.True(remaining.Vertices.Last().DistanceTo(new Point2D(10, 0)) <= 1.0e-6);
    }

    [Fact]
    public void BreakBetweenPoints_WithSamePoint_ShouldReturnNoSegments()
    {
        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        IReadOnlyList<CadEntity> result = CadBreakService.BreakBetweenPoints(
            circle,
            new Point2D(10, 0),
            new Point2D(10, 0));

        Assert.Empty(result);
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
