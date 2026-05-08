using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class TangentSnapProviderTests
{
    [Fact]
    public void Snap_WithTangentEnabledAndBasePoint_ShouldReturnTangentPointOnCircle()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 8.66),
            tolerance: 1,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(20, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Tangent, result.Kind);
        Assert.Equal(circle.Id, result.EntityId);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(Math.Sqrt(75), result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_WithTangentEnabled_ShouldReturnNearestTangentPoint()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, -8.66),
            tolerance: 1,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(20, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Tangent, result.Kind);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(-Math.Sqrt(75), result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_WithTangentEnabledButNoBasePoint_ShouldReturnNull()
    {
        var document = new CadDocument();

        document.AddEntity(
            new CircleEntity(
                new Point2D(0, 0),
                10));

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 8.66),
            tolerance: 1,
            enabledSnaps: SnapKind.Tangent);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithBasePointInsideCircle_ShouldReturnNull()
    {
        var document = new CadDocument();

        document.AddEntity(
            new CircleEntity(
                new Point2D(0, 0),
                10));

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10, 0),
            tolerance: 20,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(5, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithBasePointOnCircle_ShouldReturnNull()
    {
        var document = new CadDocument();

        document.AddEntity(
            new CircleEntity(
                new Point2D(0, 0),
                10));

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10, 0),
            tolerance: 20,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(10, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithTangentEnabledButOutsideTolerance_ShouldReturnNull()
    {
        var document = new CadDocument();

        document.AddEntity(
            new CircleEntity(
                new Point2D(0, 0),
                10));

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(0, 20),
            tolerance: 1,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(20, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithTangentEnabledOnArc_ShouldReturnOnlyPointOnArc()
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
            cursorPoint: new Point2D(5, 8.66),
            tolerance: 1,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(20, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Tangent, result.Kind);
        Assert.Equal(arc.Id, result.EntityId);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(Math.Sqrt(75), result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_WithTangentEnabledOnArc_ShouldIgnorePointOutsideArc()
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
            cursorPoint: new Point2D(5, -8.66),
            tolerance: 1,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(20, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_ShouldIgnoreInvisibleCircle()
    {
        var document = new CadDocument();

        document.AddEntity(
            new CircleEntity(
                new Point2D(0, 0),
                10,
                isVisible: false));

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 8.66),
            tolerance: 1,
            enabledSnaps: SnapKind.Tangent,
            basePoint: new Point2D(20, 0));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }
}