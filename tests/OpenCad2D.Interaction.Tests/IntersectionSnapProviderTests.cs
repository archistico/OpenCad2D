using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class IntersectionSnapProviderTests
{
    [Fact]
    public void Snap_LineLineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 10));

        var second = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 0));

        document.AddEntity(first);
        document.AddEntity(second);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5.1, 5.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(5, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_LinePolylineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 15));

        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        document.AddEntity(line);
        document.AddEntity(polyline);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5.1, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(0, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_PolylinePolylineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var first = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        var second = new PolylineEntity(new[]
        {
            new Point2D(5, -5),
            new Point2D(5, 5)
        });

        document.AddEntity(first);
        document.AddEntity(second);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5.1, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(0, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_LineCircleIntersection_ShouldReturnNearestIntersection()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(-20, 0),
            new Point2D(20, 0));

        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);

        document.AddEntity(line);
        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(9.8, 0.2),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(10, result.Point.X, precision: 10);
        Assert.Equal(0, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_CircleCircleIntersection_ShouldReturnNearestIntersection()
    {
        var document = new CadDocument();

        var first = new CircleEntity(
            new Point2D(0, 0),
            10);

        var second = new CircleEntity(
            new Point2D(10, 0),
            10);

        document.AddEntity(first);
        document.AddEntity(second);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 8.5),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(Math.Sqrt(75), result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_LineArcIntersection_ShouldReturnIntersectionOnArcOnly()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(-20, 0),
            new Point2D(20, 0));

        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        document.AddEntity(line);
        document.AddEntity(arc);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10, 0),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(10, result.Point.X, precision: 10);
        Assert.Equal(0, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_CircleArcIntersection_ShouldReturnIntersectionOnArcOnly()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(10, 0),
            10);

        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        document.AddEntity(circle);
        document.AddEntity(arc);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 8.5),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(5, result.Point.X, precision: 10);
        Assert.Equal(Math.Sqrt(75), result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_IntersectionDisabled_ShouldReturnNull()
    {
        var document = new CadDocument();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 10));

        var second = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 0));

        document.AddEntity(first);
        document.AddEntity(second);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 5),
            tolerance: 1,
            enabledSnaps: SnapKind.Endpoint);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_ShouldIgnoreInvisibleEntitiesInIntersection()
    {
        var document = new CadDocument();

        var visible = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 10));

        var invisible = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 0),
            isVisible: false);

        document.AddEntity(visible);
        document.AddEntity(invisible);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5, 5),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }
}