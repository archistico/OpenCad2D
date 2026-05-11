using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadTrimServiceTests
{
    [Fact]
    public void TrimLine_ByCircleBoundary_ShouldKeepSideAwayFromPickedPoint()
    {
        var target = new LineEntity(new Point2D(-10, 0), new Point2D(10, 0));
        var boundary = new CircleEntity(new Point2D(0, 0), 5);

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(0, 0));

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.IsType<LineEntity>(entity));
    }

    [Fact]
    public void TrimCircle_ByLineBoundary_ShouldCreateArcFragments()
    {
        var target = new CircleEntity(new Point2D(0, 0), 5);
        var boundary = new LineEntity(new Point2D(0, -10), new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        ArcEntity arc = Assert.Single(result.OfType<ArcEntity>());

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.Radius, arc.Radius);
    }

    [Fact]
    public void TrimArc_ByLineBoundary_ShouldCreateArcFragment()
    {
        var target = new ArcEntity(
            new Point2D(0, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));
        var boundary = new LineEntity(new Point2D(0, -10), new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(4, 3));

        ArcEntity arc = Assert.Single(result.OfType<ArcEntity>());

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.Radius, arc.Radius);
    }

    [Fact]
    public void TrimPolyline_ByLineBoundary_ShouldCreatePolylineFragments()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        var boundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(8, 0));

        PolylineEntity polyline = Assert.Single(result.OfType<PolylineEntity>());

        Assert.Equal(new Point2D(0, 0), polyline.Vertices[0]);
        Assert.Equal(new Point2D(5, 0), polyline.Vertices[^1]);
    }
}
