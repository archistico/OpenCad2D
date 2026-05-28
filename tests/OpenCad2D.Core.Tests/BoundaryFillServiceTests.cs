using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class BoundaryFillServiceTests
{
    [Fact]
    public void CreateFilledPolyline_FromFourLines_ShouldReturnFilledClosedPolyline()
    {
        var service = new BoundaryFillService();
        IReadOnlyList<LineEntity> boundaries = CreateRectangleLines();

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(5, 2),
            LayerId.Default);

        Assert.True(result.Succeeded);
        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result.Polyline);
        Assert.True(polyline.IsClosed);
        Assert.True(polyline.IsFilled);
        Assert.Equal(4, polyline.Vertices.Count);
        Assert.Contains(new Point2D(0, 0), polyline.Vertices);
        Assert.Contains(new Point2D(10, 0), polyline.Vertices);
        Assert.Contains(new Point2D(10, 5), polyline.Vertices);
        Assert.Contains(new Point2D(0, 5), polyline.Vertices);
    }

    [Fact]
    public void CreateFilledPolyline_WhenSeedIsOutsideBoundary_ShouldFail()
    {
        var service = new BoundaryFillService();

        BoundaryFillResult result = service.CreateFilledPolyline(
            CreateRectangleLines(),
            new Point2D(20, 20),
            LayerId.Default);

        Assert.False(result.Succeeded);
        Assert.Null(result.Polyline);
        Assert.Equal("No closed boundary was found around the picked point.", result.Message);
    }

    [Fact]
    public void CreateFilledPolyline_FromOpenBoundary_ShouldFail()
    {
        var service = new BoundaryFillService();
        var boundaries = new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(10, 0), new Point2D(10, 5)),
            new LineEntity(new Point2D(10, 5), new Point2D(0, 5))
        };

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(5, 2),
            LayerId.Default);

        Assert.False(result.Succeeded);
        Assert.Null(result.Polyline);
    }

    [Fact]
    public void CreateFilledPolyline_WithInteriorDivider_ShouldPickContainingFace()
    {
        var service = new BoundaryFillService();
        var boundaries = CreateRectangleLines()
            .Concat(new[]
            {
                new LineEntity(new Point2D(5, 0), new Point2D(5, 5))
            })
            .ToList();

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(2, 2),
            LayerId.Default);

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result.Polyline);

        Assert.True(result.Succeeded);
        Assert.True(polyline.IsFilled);
        Assert.Contains(new Point2D(0, 0), polyline.Vertices);
        Assert.Contains(new Point2D(5, 0), polyline.Vertices);
        Assert.Contains(new Point2D(5, 5), polyline.Vertices);
        Assert.Contains(new Point2D(0, 5), polyline.Vertices);
        Assert.DoesNotContain(new Point2D(10, 0), polyline.Vertices);
        Assert.DoesNotContain(new Point2D(10, 5), polyline.Vertices);
    }

    private static IReadOnlyList<LineEntity> CreateRectangleLines()
    {
        return new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(10, 0), new Point2D(10, 5)),
            new LineEntity(new Point2D(10, 5), new Point2D(0, 5)),
            new LineEntity(new Point2D(0, 5), new Point2D(0, 0))
        };
    }
}
