using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadExtendServiceTests
{
    [Fact]
    public void ExtendLine_ToCircleBoundary_ShouldExtendPickedEnd()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(5, 0));
        var boundary = new CircleEntity(new Point2D(0, 0), 10);

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        LineEntity line = Assert.IsType<LineEntity>(result);

        Assert.Equal(target.Start, line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    [Fact]
    public void ExtendArc_ToLineBoundary_ShouldExtendPickedEnd()
    {
        var target = new ArcEntity(
            new Point2D(0, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));
        var boundary = new LineEntity(new Point2D(-10, 0), new Point2D(10, 0));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            target.Geometry.EndPoint);

        ArcEntity arc = Assert.IsType<ArcEntity>(result);

        Assert.Equal(target.StartAngle, arc.StartAngle);
        Assert.True(arc.EndAngle.NormalizePositive().Degrees > 170);
    }

    [Fact]
    public void ExtendOpenPolyline_ToLineBoundary_ShouldExtendNearestEndpoint()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0)
        });
        var boundary = new LineEntity(new Point2D(10, -5), new Point2D(10, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result);

        Assert.Equal(new Point2D(0, 0), polyline.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), polyline.Vertices[^1]);
    }

    [Fact]
    public void ExtendCircle_ShouldReturnNull()
    {
        var target = new CircleEntity(new Point2D(0, 0), 5);
        var boundary = new LineEntity(new Point2D(10, -5), new Point2D(10, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        Assert.Null(result);
    }

    [Fact]
    public void ExtendArc_ToLineBoundary_ShouldExtendPickedStart()
    {
        var target = new ArcEntity(
            new Point2D(0, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));
        var boundary = new LineEntity(new Point2D(-10, -5), new Point2D(10, -5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            target.Geometry.StartPoint);

        ArcEntity arc = Assert.IsType<ArcEntity>(result);

        Assert.True(arc.StartAngle.NormalizePositive().Degrees >= 269.9 ||
                    arc.StartAngle.NormalizePositive().Degrees < 1);
        Assert.Equal(target.EndAngle, arc.EndAngle);
    }

    [Fact]
    public void ExtendOpenPolyline_ToLineBoundary_ShouldExtendStartEndpoint()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0),
            new Point2D(5, 5)
        });
        var boundary = new LineEntity(new Point2D(-5, -5), new Point2D(-5, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(0, 0));

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result);

        Assert.Equal(new Point2D(-5, 0), polyline.Vertices[0]);
        Assert.Equal(new Point2D(5, 0), polyline.Vertices[1]);
        Assert.Equal(new Point2D(5, 5), polyline.Vertices[2]);
    }

    [Fact]
    public void ExtendClosedPolyline_ShouldReturnNull()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0),
            new Point2D(5, 5)
        }, isClosed: true);
        var boundary = new LineEntity(new Point2D(10, -5), new Point2D(10, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        Assert.Null(result);
    }

    [Fact]
    public void ExtendPoint_ShouldReturnNull()
    {
        var target = new PointEntity(new Point2D(0, 0));
        var boundary = new LineEntity(new Point2D(10, -5), new Point2D(10, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(0, 0));

        Assert.Null(result);
    }

    [Fact]
    public void ExtendEllipticalArc_ToLineBoundary_ShouldExtendPickedEndWithNativeGeometry()
    {
        var target = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI / 2.0);
        var boundary = new LineEntity(new Point2D(-10, -10), new Point2D(-10, 10));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            target.EndPoint);

        EllipticalArcEntity arc = Assert.IsType<EllipticalArcEntity>(result);

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.MajorAxis, arc.MajorAxis);
        Assert.Equal(target.MinorRadius, arc.MinorRadius);
        Assert.Equal(target.StartParameterRadians, arc.StartParameterRadians);
        Assert.True(Math.Abs(arc.EndPoint.X + 10.0) < 1e-9);
        Assert.True(Math.Abs(arc.EndPoint.Y) < 1e-9);
    }

    [Fact]
    public void ExtendEllipticalArc_ToLineBoundary_ShouldExtendPickedStartWithNativeGeometry()
    {
        var target = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI / 2.0);
        var boundary = new LineEntity(new Point2D(-20, -5), new Point2D(20, -5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            target.StartPoint);

        EllipticalArcEntity arc = Assert.IsType<EllipticalArcEntity>(result);

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.MajorAxis, arc.MajorAxis);
        Assert.Equal(target.MinorRadius, arc.MinorRadius);
        Assert.Equal(target.EndParameterRadians, arc.EndParameterRadians);
        Assert.True(Math.Abs(arc.StartPoint.X) < 1e-9);
        Assert.True(Math.Abs(arc.StartPoint.Y + 5.0) < 1e-9);
    }

    [Fact]
    public void ExtendEllipse_ShouldReturnNull()
    {
        var target = new EllipseEntity(new Point2D(0, 0), new Vector2D(10, 0), 5);
        var boundary = new LineEntity(new Point2D(20, -5), new Point2D(20, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(10, 0));

        Assert.Null(result);
    }

    [Fact]
    public void ExtendLine_ToEllipseBoundary_ShouldReuseNativeEllipseIntersectionPoint()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(5, 0));
        var boundary = new EllipseEntity(new Point2D(0, 0), new Vector2D(10, 0), 5);

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            target.End);

        LineEntity line = Assert.IsType<LineEntity>(result);

        Assert.Equal(target.Start, line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
        AssertPointOnEllipse(boundary, line.End);
    }

    [Fact]
    public void ExtendOpenPolyline_ToEllipticalArcBoundary_ShouldReuseNativeEllipseIntersectionPoint()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0)
        });
        var boundary = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            -Math.PI / 2.0,
            Math.PI / 2.0);

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result);

        Assert.Equal(new Point2D(0, 0), polyline.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), polyline.Vertices[^1]);
        AssertPointOnEllipse(boundary, polyline.Vertices[^1]);
    }

    [Fact]
    public void ExtendArc_ToEllipseBoundary_ShouldUseNativeCircleEllipseIntersection()
    {
        var target = new ArcEntity(
            new Point2D(0, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));
        var boundary = new EllipseEntity(new Point2D(0, 0), new Vector2D(10, 0), 3);

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            target.Geometry.StartPoint);

        ArcEntity arc = Assert.IsType<ArcEntity>(result);

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.Radius, arc.Radius);
        Assert.Equal(target.EndAngle, arc.EndAngle);
        Assert.True(arc.StartAngle.NormalizePositive().Degrees > 300);
        AssertPointOnEllipse(boundary, arc.Geometry.StartPoint);
        Assert.True(Math.Abs(arc.Geometry.StartPoint.DistanceTo(target.Center) - target.Radius) < 1e-9);
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
            Math.Abs(equation - 1.0) <= 1e-8,
            $"Expected point {point} to lie on the source ellipse; equation value was {equation}.");
    }



    [Fact]
    public void ExtendOpenMixedPolyline_WithStraightEndpoint_ShouldPreserveExistingBulges()
    {
        double bulge = Math.Tan(Math.PI / 8.0);
        var target = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(15, 0)
            },
            segmentBulges: new[] { bulge, 0.0 });
        var boundary = new LineEntity(new Point2D(20, -5), new Point2D(20, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(15, 0));

        PolylineEntity extended = Assert.IsType<PolylineEntity>(result);

        Assert.Equal(new Point2D(20, 0), extended.Vertices[^1]);
        Assert.Equal(target.SegmentBulges.Count, extended.SegmentBulges.Count);
        Assert.Equal(bulge, extended.SegmentBulges[0], precision: 10);
        Assert.Equal(0.0, extended.SegmentBulges[1], precision: 10);
    }

    [Fact]
    public void ExtendOpenMixedPolyline_WithCurvedEndpoint_ShouldReturnNullInsteadOfFlattening()
    {
        double bulge = Math.Tan(Math.PI / 8.0);
        var target = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { bulge });
        var boundary = new LineEntity(new Point2D(15, -5), new Point2D(15, 5));

        CadEntity? result = CadExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(10, 0));

        Assert.Null(result);
    }

}
