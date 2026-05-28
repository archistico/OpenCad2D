using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class SnapServiceTests
{
    [Fact]
    public void Snap_WithEndpointEnabled_ShouldReturnEndpoint()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(0.2, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Endpoint);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Endpoint, result.Kind);
        Assert.Equal(new Point2D(0, 0), result.Point);
        Assert.Equal(line.Id, result.EntityId);
    }

    [Fact]
    public void Snap_WithMidpointEnabled_ShouldReturnLineMidpoint()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(5.1, 0.2),
            tolerance: 1,
            enabledSnaps: SnapKind.Midpoint);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Midpoint, result.Kind);
        Assert.Equal(new Point2D(5, 0), result.Point);
    }


    [Fact]
    public void Snap_WithMidpointEnabledOnBulgedPolyline_ShouldReturnArcMidpoint()
    {
        var document = new CadDocument();

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { 1.0 });

        document.AddEntity(polyline);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(5.1, 4.9),
            tolerance: 1,
            enabledSnaps: SnapKind.Midpoint);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Midpoint, result.Kind);
        Assert.Equal(polyline.Id, result.EntityId);
        Assert.Equal(5, result.Point.X, precision: 2);
        Assert.Equal(5, result.Point.Y, precision: 2);
    }

    [Fact]
    public void Snap_WithCenterEnabled_ShouldReturnCircleCenter()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(10, 20),
            5);

        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(10.3, 20.2),
            tolerance: 1,
            enabledSnaps: SnapKind.Center);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Center, result.Kind);
        Assert.Equal(new Point2D(10, 20), result.Point);
    }

    [Fact]
    public void Snap_WithNearestEnabled_ShouldReturnClosestPointOnLine()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(4, 0.3),
            tolerance: 1,
            enabledSnaps: SnapKind.Nearest);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Nearest, result.Kind);
        Assert.Equal(new Point2D(4, 0), result.Point);
    }

    [Fact]
    public void Snap_WithPerpendicularEnabledAndBasePoint_ShouldReturnProjectionOnLine()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5.1, 0.2),
            tolerance: 1,
            enabledSnaps: SnapKind.Perpendicular,
            basePoint: new Point2D(5, 5));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Perpendicular, result.Kind);
        Assert.Equal(new Point2D(5, 0), result.Point);
    }

    [Fact]
    public void Snap_WithPerpendicularEnabledButWithoutBasePoint_ShouldReturnNull()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 0),
            tolerance: 1,
            enabledSnaps: SnapKind.Perpendicular);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithPerpendicularEnabledAndProjectionOutsideSegment_ShouldReturnNull()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(20, 0),
            tolerance: 1,
            enabledSnaps: SnapKind.Perpendicular,
            basePoint: new Point2D(20, 5));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithPerpendicularEnabledOnCircle_ShouldReturnRadialPoint()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10.1, 0.2),
            tolerance: 1,
            enabledSnaps: SnapKind.Perpendicular,
            basePoint: new Point2D(20, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Perpendicular, result.Kind);
        Assert.Equal(10, result.Point.X, precision: 10);
        Assert.Equal(0, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_ShouldIgnoreInvisibleEntities()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            isVisible: false);

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(0, 0),
            tolerance: 1,
            enabledSnaps: SnapKind.Endpoint | SnapKind.Nearest);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithEndpointAndNearestEnabled_ShouldPreferEndpoint()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(0.1, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Endpoint | SnapKind.Nearest);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Endpoint, result.Kind);
        Assert.Equal(new Point2D(0, 0), result.Point);
    }

    [Fact]
    public void Snap_WithQuadrantEnabledOnCircle_ShouldReturnRightQuadrant()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(10, 10),
            5);

        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(15.2, 10.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Quadrant);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Quadrant, result.Kind);
        Assert.Equal(15, result.Point.X, precision: 10);
        Assert.Equal(10, result.Point.Y, precision: 10);
        Assert.Equal(circle.Id, result.EntityId);
    }

    [Fact]
    public void Snap_WithQuadrantEnabledOnCircle_ShouldReturnTopQuadrant()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(10, 10),
            5);

        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10.1, 15.2),
            tolerance: 1,
            enabledSnaps: SnapKind.Quadrant);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Quadrant, result.Kind);
        Assert.Equal(10, result.Point.X, precision: 10);
        Assert.Equal(15, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_WithQuadrantEnabledOnArc_ShouldReturnQuadrantInsideArc()
    {
        var document = new CadDocument();

        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        document.AddEntity(arc);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(0.2, 10.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Quadrant);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Quadrant, result.Kind);
        Assert.Equal(0, result.Point.X, precision: 10);
        Assert.Equal(10, result.Point.Y, precision: 10);
        Assert.Equal(arc.Id, result.EntityId);
    }

    [Fact]
    public void Snap_WithQuadrantEnabledOnArc_ShouldIgnoreQuadrantOutsideArc()
    {
        var document = new CadDocument();

        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        document.AddEntity(arc);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(0.1, -10.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Quadrant);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithQuadrantDisabled_ShouldReturnNullNearCircleQuadrant()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10.1, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Center);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_ShouldIgnoreEntity_WhenLayerIsHidden()
    {
        var document = new CadDocument();

        var hiddenLayer = new Layer(
            new LayerId("Hidden"),
            "Hidden",
            isVisible: false);

        document.Layers.Add(hiddenLayer);

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: hiddenLayer.Id);

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(0, 0),
            tolerance: 1,
            enabledSnaps: SnapKind.Endpoint | SnapKind.Nearest);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }
}