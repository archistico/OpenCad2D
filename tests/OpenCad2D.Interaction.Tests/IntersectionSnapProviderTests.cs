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
    public void Snap_LineBulgedPolylineIntersection_ShouldUseArcGeometry()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(5, 4),
            new Point2D(5, 6));

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { 1.0 });

        document.AddEntity(line);
        document.AddEntity(polyline);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(5.05, 5.05),
            tolerance: 0.5,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(5, result.Point.X, precision: 2);
        Assert.Equal(5, result.Point.Y, precision: 1);
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
    public void Snap_LineArcIntersection_WithClockwiseArcCrossingZero_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(-20, 0),
            new Point2D(20, 0));

        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(10),
            Angle.FromDegrees(350),
            isCounterClockwise: false);

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
    public void Snap_CircleArcIntersection_WithClockwiseArcCrossingZero_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(20, 0),
            10);

        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(10),
            Angle.FromDegrees(350),
            isCounterClockwise: false);

        document.AddEntity(circle);
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
    public void Snap_LineEllipseIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(-20, 0),
            new Point2D(20, 0));

        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);

        document.AddEntity(line);
        document.AddEntity(ellipse);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10.1, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(10, result.Point.X, precision: 10);
        Assert.Equal(0, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_PolylineEllipseIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, -10),
            new Point2D(0, 10)
        });

        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);

        document.AddEntity(polyline);
        document.AddEntity(ellipse);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(0.1, 5.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(0, result.Point.X, precision: 10);
        Assert.Equal(5, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_LineSplineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));

        var spline = new BezierSplineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        document.AddEntity(line);
        document.AddEntity(spline);

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
    public void Snap_EllipseSplineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);

        var spline = new BezierSplineEntity(new[]
        {
            new Point2D(10, -5),
            new Point2D(10, 5)
        });

        document.AddEntity(ellipse);
        document.AddEntity(spline);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10.1, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(10, result.Point.X, precision: 10);
        Assert.Equal(0, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_CircleClosedRectanglePolylineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var rectangle = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true);

        var circle = new CircleEntity(
            new Point2D(5, 5),
            Math.Sqrt(34));

        document.AddEntity(rectangle);
        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(10.05, 8.05),
            tolerance: 0.25,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(10, result.Point.X, precision: 10);
        Assert.Equal(8, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_CircleRotatedClosedRectanglePolylineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var rectangle = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(8, 6),
                new Point2D(4.4, 10.8),
                new Point2D(-3.6, 4.8)
            },
            isClosed: true);

        var circle = new CircleEntity(
            new Point2D(4, 3),
            3);

        document.AddEntity(rectangle);
        document.AddEntity(circle);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(6.4, 4.8),
            tolerance: 0.25,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(6.4, result.Point.X, precision: 10);
        Assert.Equal(4.8, result.Point.Y, precision: 10);
    }


    [Fact]
    public void Snap_ArcArcIntersection_ShouldReturnNearestIntersection()
    {
        var document = new CadDocument();

        var first = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        var second = new ArcEntity(
            new Point2D(10, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

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
    public void Snap_ArcPolylineIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var arc = new ArcEntity(
            new Point2D(0, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, -10),
            new Point2D(0, 10)
        });

        document.AddEntity(arc);
        document.AddEntity(polyline);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(0.1, 5.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(0, result.Point.X, precision: 10);
        Assert.Equal(5, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_LineEllipticalArcIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, -10),
            new Point2D(0, 10));

        var ellipticalArc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);

        document.AddEntity(line);
        document.AddEntity(ellipticalArc);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(0.1, 5.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(0, result.Point.X, precision: 10);
        Assert.Equal(5, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_PolylineEllipticalArcIntersection_ShouldReturnIntersection()
    {
        var document = new CadDocument();

        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, -10),
            new Point2D(0, 10)
        });

        var ellipticalArc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);

        document.AddEntity(polyline);
        document.AddEntity(ellipticalArc);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(0.1, 5.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(0, result.Point.X, precision: 10);
        Assert.Equal(5, result.Point.Y, precision: 10);
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
public sealed class StairIntersectionSnapProviderTests
{
    [Fact]
    public void Snap_LineStairIntersection_ShouldReturnIntersectionOnGeneratedLinework()
    {
        var document = new CadDocument();
        var stair = new StairEntity(
            new Point2D(0, 0),
            OpenCad2D.Core.Architecture.Stairs.StairViewKind.Plan,
            width: 1,
            treadCount: 3,
            treadDepth: 1,
            riserHeight: 0.2);
        var line = new LineEntity(
            new Point2D(1, -1),
            new Point2D(1, 2));

        document.AddEntity(stair);
        document.AddEntity(line);

        var service = new SnapService();
        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(1, 1),
            tolerance: 0.2,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(1, result.Point.X, precision: 10);
        Assert.Equal(1, result.Point.Y, precision: 10);
    }

    [Fact]
    public void Snap_StairStairIntersection_ShouldReturnIntersectionBetweenGeneratedLinework()
    {
        var document = new CadDocument();
        var first = new StairEntity(
            new Point2D(0, 0),
            OpenCad2D.Core.Architecture.Stairs.StairViewKind.Plan,
            width: 1,
            treadCount: 3,
            treadDepth: 1,
            riserHeight: 0.2);
        var second = new StairEntity(
            new Point2D(1, -0.5),
            OpenCad2D.Core.Architecture.Stairs.StairViewKind.FrontElevation,
            width: 2,
            treadCount: 2,
            treadDepth: 1,
            riserHeight: 1);

        document.AddEntity(first);
        document.AddEntity(second);

        var service = new SnapService();
        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(2, 0.5),
            tolerance: 0.2,
            enabledSnaps: SnapKind.Intersection);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Intersection, result.Kind);
        Assert.Equal(2, result.Point.X, precision: 10);
        Assert.Equal(0.5, result.Point.Y, precision: 10);
    }
}
