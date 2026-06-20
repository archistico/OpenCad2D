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
        Assert.Equal(seedPoint, result.SeedPoint);
        Assert.Equal(4, result.BoundaryVertices.Count);
        Assert.Equal(4, result.Diagnostics.SourceSegmentCount);
        Assert.True(result.Diagnostics.GraphEdgeCount >= 4);
        Assert.True(result.Diagnostics.CandidateFaceCount >= 1);
    }

    [Fact]
    public void CreateFilledPolyline_WithOnlyUnsupportedEntities_ShouldReturnUnsupportedOnlyStatus()
    {
        var service = new BoundaryFillService();
        var boundaries = new CadEntity[]
        {
            new CircleEntity(new Point2D(0, 0), 5)
        };

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(0, 0),
            LayerId.Default);

        Assert.False(result.Succeeded);
        Assert.Equal(BoundaryFillStatus.UnsupportedOnly, result.Status);
        Assert.Equal(1, result.Diagnostics.IgnoredEntityCount);
        Assert.Equal(0, result.Diagnostics.SourceSegmentCount);
    }

    [Fact]
    public void CreateFilledPolyline_ShouldKeepGapToleranceInDiagnostics()
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
    public void CreateFilledPolyline_FromCircle_WhenCurveBoundariesEnabled_ShouldCreateFilledPolyline()
    {
        var service = new BoundaryFillService();
        var boundaries = new CadEntity[]
        {
            new CircleEntity(new Point2D(0, 0), 10)
        };
        var options = new BoundaryFillOptions(
            includeCurveBoundaries: true,
            curveSampleCount: 32);

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(0, 0),
            LayerId.Default,
            options);

        Assert.True(result.Succeeded);
        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result.Polyline);
        Assert.True(polyline.IsClosed);
        Assert.True(polyline.IsFilled);
        Assert.True(polyline.Vertices.Count >= 16);
        Assert.Equal(32, result.Diagnostics.SampledCurveSegmentCount);
    }

    [Fact]
    public void CreateFilledPolyline_FromArcAndLine_WhenCurveBoundariesEnabled_ShouldCreateFilledPolyline()
    {
        var service = new BoundaryFillService();
        var boundaries = new CadEntity[]
        {
            new ArcEntity(
                new Point2D(0, 0),
                10,
                Angle.FromDegrees(0),
                Angle.FromDegrees(180)),
            new LineEntity(new Point2D(-10, 0), new Point2D(10, 0))
        };
        var options = new BoundaryFillOptions(
            includeCurveBoundaries: true,
            curveSampleCount: 32);

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(0, 2),
            LayerId.Default,
            options);

        Assert.True(result.Succeeded);
        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result.Polyline);
        Assert.True(polyline.IsClosed);
        Assert.True(polyline.IsFilled);
        Assert.True(result.Diagnostics.SampledCurveSegmentCount > 0);
    }

    [Fact]
    public void CreateFilledPolyline_WithSmallEndpointGapWithinTolerance_ShouldBridgeGap()
    {
        var service = new BoundaryFillService();
        var boundaries = new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(10, 0), new Point2D(10, 5)),
            new LineEntity(new Point2D(10, 5), new Point2D(0, 5)),
            new LineEntity(new Point2D(0, 5), new Point2D(0, 0.2))
        };
        var options = new BoundaryFillOptions(gapTolerance: 0.25);

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(5, 2),
            LayerId.Default,
            options);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Diagnostics.BridgedGapCount);
        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result.Polyline);
        Assert.True(polyline.IsClosed);
        Assert.True(polyline.IsFilled);
    }


    [Fact]
    public void CreateFilledPolyline_WithSampleBoundaryGap_ShouldPreserveExistingHorizontalEndpoint()
    {
        var service = new BoundaryFillService();
        var boundaries = new[]
        {
            new LineEntity(new Point2D(150, 100), new Point2D(300, 100)),
            new LineEntity(new Point2D(150, 100), new Point2D(150, 200)),
            new LineEntity(new Point2D(150, 200), new Point2D(300, 200)),
            new LineEntity(new Point2D(300, 200), new Point2D(300, 100.30422638643512))
        };
        var options = new BoundaryFillOptions(gapTolerance: 0.5);

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(220, 150),
            LayerId.Default,
            options);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Diagnostics.BridgedGapCount);
        PolylineEntity polyline = Assert.IsType<PolylineEntity>(result.Polyline);
        Assert.Contains(polyline.Vertices, point =>
            point.DistanceTo(new Point2D(300, 100)) <= 1e-9);
        Assert.DoesNotContain(polyline.Vertices, point =>
            point.DistanceTo(new Point2D(300, 100.15211319321756)) <= 1e-9);
    }

    [Fact]
    public void CreateFilledPolyline_WithEndpointGapAboveTolerance_ShouldFail()
    {
        var service = new BoundaryFillService();
        var boundaries = new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(10, 0), new Point2D(10, 5)),
            new LineEntity(new Point2D(10, 5), new Point2D(0, 5)),
            new LineEntity(new Point2D(0, 5), new Point2D(0, 0.5))
        };
        var options = new BoundaryFillOptions(gapTolerance: 0.25);

        BoundaryFillResult result = service.CreateFilledPolyline(
            boundaries,
            new Point2D(5, 2),
            LayerId.Default,
            options);

        Assert.False(result.Succeeded);
        Assert.Equal(BoundaryFillStatus.NoClosedBoundary, result.Status);
        Assert.Equal(0, result.Diagnostics.BridgedGapCount);
    }

    [Fact]
    public void CreateFilledPolyline_FromClosedBoundary_ShouldNotReportBridgedGaps()
    {
        var service = new BoundaryFillService();
        var options = new BoundaryFillOptions(gapTolerance: 0.25);

        BoundaryFillResult result = service.CreateFilledPolyline(
            CreateRectangleLines(),
            new Point2D(5, 2),
            LayerId.Default,
            options);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Diagnostics.BridgedGapCount);
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
