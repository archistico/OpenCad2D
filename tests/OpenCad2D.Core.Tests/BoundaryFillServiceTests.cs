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


    [Fact]
    public void CreateFilledPolyline_ShouldExposeStatusSeedBoundaryAndDiagnostics()
    {
        var service = new BoundaryFillService();
        Point2D seedPoint = new(5, 2);

        BoundaryFillResult result = service.CreateFilledPolyline(
            CreateRectangleLines(),
            seedPoint,
            LayerId.Default);

        Assert.True(result.Succeeded);
        Assert.Equal(BoundaryFillStatus.Success, result.Status);
        Assert.True(result.SeedPoint.HasValue);
        Assert.Equal(seedPoint, result.SeedPoint.Value);
        Assert.Equal(4, result.BoundaryVertices.Count);
        Assert.Equal(4, result.Diagnostics.SourceSegmentCount);
        Assert.Equal(4, result.Diagnostics.GraphEdgeCount);
        Assert.True(result.Diagnostics.CandidateFaceCount >= 1);
        Assert.Equal(0, result.Diagnostics.IgnoredEntityCount);
        Assert.Equal(0, result.Diagnostics.BridgedGapCount);
        Assert.Equal(0, result.Diagnostics.SampledCurveSegmentCount);
    }

    [Fact]
    public void CreateFilledPolyline_WithOptions_ShouldExposeGapToleranceInDiagnostics()
    {
        var service = new BoundaryFillService();
        var options = new BoundaryFillOptions(gapTolerance: 0.25);

        BoundaryFillResult result = service.CreateFilledPolyline(
            CreateRectangleLines(),
            new Point2D(5, 2),
            LayerId.Default,
            options);

        Assert.True(result.Succeeded);
        Assert.Equal(0.25, result.Diagnostics.GapTolerance);
    }

    [Fact]
    public void CreateFilledPolyline_WithOnlyUnsupportedBoundaries_ShouldReportUnsupportedOnly()
    {
        var service = new BoundaryFillService();
        var boundaries = new CadEntity[]
        {
            new CircleEntity(new Point2D(0, 0), 10)
        };

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(0, 0),
            LayerId.Default);

        Assert.False(result.Succeeded);
        Assert.Equal(BoundaryFillStatus.UnsupportedOnly, result.Status);
        Assert.Equal(1, result.Diagnostics.IgnoredEntityCount);
        Assert.Equal(0, result.Diagnostics.SourceSegmentCount);
        Assert.Equal("Boundary fill needs visible line or straight polyline boundaries.", result.Message);
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
