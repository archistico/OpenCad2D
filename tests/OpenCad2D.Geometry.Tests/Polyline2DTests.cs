using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class Polyline2DTests
{
    [Fact]
    public void Constructor_WithLessThanTwoVertices_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new Polyline2D(new[]
            {
                new Point2D(0, 0)
            }));
    }

    [Fact]
    public void Constructor_WithTwoVertices_ShouldCreatePolyline()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        Assert.Equal(2, polyline.VertexCount);
        Assert.False(polyline.IsClosed);
    }

    [Fact]
    public void SegmentCount_ForOpenPolyline_ShouldReturnVertexCountMinusOne()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        Assert.Equal(2, polyline.SegmentCount);
    }

    [Fact]
    public void SegmentCount_ForClosedPolyline_ShouldReturnVertexCount()
    {
        var polyline = new Polyline2D(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: true);

        Assert.Equal(3, polyline.SegmentCount);
    }

    [Fact]
    public void GetSegments_ForOpenPolyline_ShouldReturnExpectedSegments()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        IReadOnlyList<LineSegment2D> segments = polyline.GetSegments();

        Assert.Equal(2, segments.Count);
        Assert.Equal(new Point2D(0, 0), segments[0].Start);
        Assert.Equal(new Point2D(10, 0), segments[0].End);
        Assert.Equal(new Point2D(10, 0), segments[1].Start);
        Assert.Equal(new Point2D(10, 10), segments[1].End);
    }

    [Fact]
    public void GetSegments_ForClosedPolyline_ShouldAddClosingSegment()
    {
        var polyline = new Polyline2D(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: true);

        IReadOnlyList<LineSegment2D> segments = polyline.GetSegments();

        Assert.Equal(3, segments.Count);
        Assert.Equal(new Point2D(10, 10), segments[2].Start);
        Assert.Equal(new Point2D(0, 0), segments[2].End);
    }

    [Fact]
    public void Length_ForOpenPolyline_ShouldReturnSumOfSegments()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(6, 8)
        });

        Assert.Equal(10, polyline.Length);
    }

    [Fact]
    public void Length_ForClosedPolyline_ShouldIncludeClosingSegment()
    {
        var polyline = new Polyline2D(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true);

        Assert.Equal(40, polyline.Length);
    }

    [Fact]
    public void GetBoundingBox_ShouldReturnCorrectBounds()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(5, 10),
            new Point2D(-3, 20),
            new Point2D(15, -2)
        });

        BoundingBox2D box = polyline.GetBoundingBox();

        Assert.Equal(-3, box.MinX);
        Assert.Equal(-2, box.MinY);
        Assert.Equal(15, box.MaxX);
        Assert.Equal(20, box.MaxY);
    }

    [Fact]
    public void ContainsVertex_WhenVertexExists_ShouldReturnTrue()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        bool result = polyline.ContainsVertex(new Point2D(10, 0));

        Assert.True(result);
    }

    [Fact]
    public void ContainsVertex_WhenVertexDoesNotExist_ShouldReturnFalse()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        bool result = polyline.ContainsVertex(new Point2D(5, 0));

        Assert.False(result);
    }

    [Fact]
    public void Reverse_ShouldReverseVertices()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(20, 0)
        });

        Polyline2D reversed = polyline.Reverse();

        Assert.Equal(new Point2D(20, 0), reversed.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), reversed.Vertices[1]);
        Assert.Equal(new Point2D(0, 0), reversed.Vertices[2]);
    }

    [Fact]
    public void AddVertex_ShouldReturnNewPolylineWithAdditionalVertex()
    {
        var polyline = new Polyline2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        Polyline2D result = polyline.AddVertex(new Point2D(20, 0));

        Assert.Equal(3, result.VertexCount);
        Assert.Equal(2, polyline.VertexCount);
    }
}