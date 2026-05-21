using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class LineModifyServiceTests
{
    [Fact]
    public void GetParameter_ShouldReturnNormalizedParameterOnSegment()
    {
        var segment = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        double parameter = LineParameterService.GetParameter(
            segment,
            new Point2D(4, 0));

        Assert.Equal(0.4, parameter, precision: 12);
    }

    [Fact]
    public void BreakAtPoint_ShouldSplitLineIntoTwoSegments()
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        IReadOnlyList<LineEntity> result = LineBreakService.BreakAtPoint(
            line,
            new Point2D(4, 1));

        Assert.Equal(2, result.Count);
        Assert.Equal(new Point2D(0, 0), result[0].Start);
        Assert.Equal(new Point2D(4, 0), result[0].End);
        Assert.Equal(new Point2D(4, 0), result[1].Start);
        Assert.Equal(new Point2D(10, 0), result[1].End);
        Assert.All(result, part => Assert.Equal(line.LayerId, part.LayerId));
    }

    [Fact]
    public void BreakAtPoint_WhenPointProjectsToEndpoint_ShouldReturnEmpty()
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        IReadOnlyList<LineEntity> result = LineBreakService.BreakAtPoint(
            line,
            new Point2D(0, 0));

        Assert.Empty(result);
    }

    [Fact]
    public void BreakBetweenPoints_ShouldRemoveMiddleSegmentAndReturnOuterSegments()
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        IReadOnlyList<LineEntity> result = LineBreakService.BreakBetweenPoints(
            line,
            new Point2D(7, 1),
            new Point2D(3, -1));

        Assert.Equal(2, result.Count);
        Assert.Equal(new Point2D(0, 0), result[0].Start);
        Assert.Equal(new Point2D(3, 0), result[0].End);
        Assert.Equal(new Point2D(7, 0), result[1].Start);
        Assert.Equal(new Point2D(10, 0), result[1].End);
    }

    [Fact]
    public void BreakBetweenPoints_WhenBreakStartsAtLineStart_ShouldReturnOnlySecondSegment()
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        IReadOnlyList<LineEntity> result = LineBreakService.BreakBetweenPoints(
            line,
            new Point2D(0, 0),
            new Point2D(4, 0));

        Assert.Single(result);
        Assert.Equal(new Point2D(4, 0), result[0].Start);
        Assert.Equal(new Point2D(10, 0), result[0].End);
    }

    [Fact]
    public void TryIntersectInfiniteLines_ShouldReturnPointAndParameters()
    {
        var horizontal = new LineSegment2D(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var vertical = new LineSegment2D(
            new Point2D(5, -5),
            new Point2D(5, 5));

        bool found = LineIntersectionService.TryIntersectInfiniteLines(
            horizontal,
            vertical,
            out LineIntersectionInfo intersection);

        Assert.True(found);
        Assert.Equal(new Point2D(5, 0), intersection.Point);
        Assert.Equal(0.5, intersection.FirstParameter, precision: 12);
        Assert.Equal(0.5, intersection.SecondParameter, precision: 12);
    }

    [Fact]
    public void ExtendToBoundary_WhenPickedEndCanReachBoundary_ShouldExtendEnd()
    {
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));

        LineEntity? result = LineExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
        Assert.Equal(new Point2D(0, 0), result.Start);
        Assert.Equal(new Point2D(10, 0), result.End);
    }

    [Fact]
    public void ExtendToBoundary_WhenIntersectionIsInsideTarget_ShouldReturnNull()
    {
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));

        LineEntity? result = LineExtendService.ExtendToBoundary(
            target,
            boundary,
            new Point2D(10, 0));

        Assert.Null(result);
    }

    [Fact]
    public void CadTrimByBoundary_WhenPickedRightSide_ShouldKeepLeftSide()
    {
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(8, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));
        Assert.Equal(new Point2D(0, 0), kept.Start);
        Assert.Equal(new Point2D(5, 0), kept.End);
    }

    [Fact]
    public void CadTrimByBoundary_WhenPickedLeftSide_ShouldKeepRightSide()
    {
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(2, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));
        Assert.Equal(new Point2D(5, 0), kept.Start);
        Assert.Equal(new Point2D(10, 0), kept.End);
    }

    [Fact]
    public void CadTrimByBoundary_WhenBoundaryDoesNotIntersectTargetSegment_ShouldReturnEmpty()
    {
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var boundary = new LineEntity(
            new Point2D(15, -5),
            new Point2D(15, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(8, 0));

        Assert.Empty(result);
    }
}
