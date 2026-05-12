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

public sealed class CadTrimServiceTwoBoundaryTests
{
    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenPickedMiddle_ShouldReturnTwoOuterFragments()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var leftBoundary = new LineEntity(new Point2D(3, -5), new Point2D(3, 5));
        var rightBoundary = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(5, 0));

        Assert.Equal(2, result.Count);

        LineEntity first = Assert.IsType<LineEntity>(result[0]);
        LineEntity second = Assert.IsType<LineEntity>(result[1]);

        Assert.Equal(new Point2D(0, 0), first.Start);
        Assert.Equal(new Point2D(3, 0), first.End);
        Assert.Equal(new Point2D(7, 0), second.Start);
        Assert.Equal(new Point2D(10, 0), second.End);
    }

    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenPickedLeftOuterSide_ShouldReturnSingleRightFragment()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var leftBoundary = new LineEntity(new Point2D(3, -5), new Point2D(3, 5));
        var rightBoundary = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(1, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(3, 0), kept.Start);
        Assert.Equal(new Point2D(10, 0), kept.End);
    }

    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenPickedRightOuterSide_ShouldReturnSingleLeftFragment()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var leftBoundary = new LineEntity(new Point2D(3, -5), new Point2D(3, 5));
        var rightBoundary = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(9, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(0, 0), kept.Start);
        Assert.Equal(new Point2D(7, 0), kept.End);
    }

    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenOnlyOneBoundaryIntersects_ShouldBehaveLikeSingleBoundaryTrim()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var intersectingBoundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));
        var externalBoundary = new LineEntity(new Point2D(20, -5), new Point2D(20, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { intersectingBoundary, externalBoundary },
            new Point2D(8, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(0, 0), kept.Start);
        Assert.Equal(new Point2D(5, 0), kept.End);
    }
}
