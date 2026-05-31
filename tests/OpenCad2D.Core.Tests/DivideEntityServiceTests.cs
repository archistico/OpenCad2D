using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class DivideEntityServiceTests
{
    private readonly DivideEntityService _service = new();

    [Fact]
    public void Divide_LineIntoThreeSegments_ShouldReturnTwoInternalPoints()
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(300, 0));

        DivideEntityResult result = _service.Divide(line, 3);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Points.Count);
        AssertPoint(new Point2D(100, 0), result.Points[0]);
        AssertPoint(new Point2D(200, 0), result.Points[1]);
    }

    [Fact]
    public void Divide_DiagonalLineIntoTwoSegments_ShouldReturnMidpoint()
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 10));

        DivideEntityResult result = _service.Divide(line, 2);

        Assert.True(result.Succeeded);
        Point2D point = Assert.Single(result.Points);
        AssertPoint(new Point2D(5, 5), point);
    }

    [Fact]
    public void Divide_ArcIntoTwoSegments_ShouldReturnArcMidpoint()
    {
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        DivideEntityResult result = _service.Divide(arc, 2);

        Assert.True(result.Succeeded);
        Point2D point = Assert.Single(result.Points);
        AssertPoint(new Point2D(0, 10), point);
    }

    [Fact]
    public void Divide_CircleIntoFourSegments_ShouldReturnFourCardinalPoints()
    {
        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        DivideEntityResult result = _service.Divide(circle, 4);

        Assert.True(result.Succeeded);
        Assert.Equal(4, result.Points.Count);
        AssertPoint(new Point2D(10, 0), result.Points[0]);
        AssertPoint(new Point2D(0, 10), result.Points[1]);
        AssertPoint(new Point2D(-10, 0), result.Points[2]);
        AssertPoint(new Point2D(0, -10), result.Points[3]);
    }

    [Fact]
    public void Divide_OpenPolyline_ShouldUseCumulativeLength()
    {
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(50, 0),
            new Point2D(300, 0)
        });

        DivideEntityResult result = _service.Divide(polyline, 3);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Points.Count);
        AssertPoint(new Point2D(100, 0), result.Points[0]);
        AssertPoint(new Point2D(200, 0), result.Points[1]);
    }

    [Fact]
    public void Divide_ClosedPolyline_ShouldReturnOnePointPerSegmentCountStartingAtFirstVertex()
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

        DivideEntityResult result = _service.Divide(polyline, 4);

        Assert.True(result.Succeeded);
        Assert.Equal(4, result.Points.Count);
        AssertPoint(new Point2D(0, 0), result.Points[0]);
        AssertPoint(new Point2D(10, 0), result.Points[1]);
        AssertPoint(new Point2D(10, 10), result.Points[2]);
        AssertPoint(new Point2D(0, 10), result.Points[3]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1001)]
    public void Divide_WithInvalidSegmentCount_ShouldFail(int segmentCount)
    {
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        DivideEntityResult result = _service.Divide(line, segmentCount);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Points);
    }

    [Fact]
    public void Divide_UnsupportedEntity_ShouldFail()
    {
        var point = new PointEntity(new Point2D(0, 0));

        DivideEntityResult result = _service.Divide(point, 2);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Points);
    }

    [Fact]
    public void Divide_ZeroLengthLine_ShouldFail()
    {
        var line = new LineEntity(
            new Point2D(5, 5),
            new Point2D(5, 5));

        DivideEntityResult result = _service.Divide(line, 2);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Points);
    }

    private static void AssertPoint(Point2D expected, Point2D actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 8);
        Assert.Equal(expected.Y, actual.Y, precision: 8);
    }
}
