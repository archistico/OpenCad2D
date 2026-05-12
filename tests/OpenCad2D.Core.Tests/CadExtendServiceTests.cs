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

}
