using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Grips;

namespace OpenCad2D.Tools.Tests;

public sealed class GripProviderTests
{
    [Fact]
    public void LineGripProvider_GetGrips_ShouldReturnStartMidpointAndEnd()
    {
        var provider = new LineGripProvider();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        IReadOnlyList<GripPoint> grips = provider.GetGrips(line);

        Assert.Equal(3, grips.Count);
        Assert.Equal(new Point2D(0, 0), grips[0].Position);
        Assert.Equal(GripKind.MoveVertex, grips[0].Kind);
        Assert.Equal(new Point2D(5, 0), grips[1].Position);
        Assert.Equal(GripKind.MoveEntity, grips[1].Kind);
        Assert.Equal(new Point2D(10, 0), grips[2].Position);
        Assert.Equal(GripKind.MoveVertex, grips[2].Kind);
    }

    [Fact]
    public void LineGripProvider_ApplyGripMove_StartGrip_ShouldMoveStartAndPreserveId()
    {
        var provider = new LineGripProvider();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var result = (LineEntity)provider.ApplyGripMove(
            line,
            0,
            new Point2D(2, 3));

        Assert.Equal(line.Id, result.Id);
        Assert.Equal(new Point2D(2, 3), result.Start);
        Assert.Equal(new Point2D(10, 0), result.End);
    }

    [Fact]
    public void LineGripProvider_ApplyGripMove_MidpointGrip_ShouldMoveWholeLine()
    {
        var provider = new LineGripProvider();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var result = (LineEntity)provider.ApplyGripMove(
            line,
            1,
            new Point2D(7, 2));

        Assert.Equal(line.Id, result.Id);
        Assert.Equal(new Point2D(2, 2), result.Start);
        Assert.Equal(new Point2D(12, 2), result.End);
    }

    [Fact]
    public void CircleGripProvider_GetGrips_ShouldReturnCenterAndQuadrants()
    {
        var provider = new CircleGripProvider();
        var circle = new CircleEntity(
            new Point2D(10, 20),
            5);

        IReadOnlyList<GripPoint> grips = provider.GetGrips(circle);

        Assert.Equal(5, grips.Count);
        Assert.Equal(new Point2D(10, 20), grips[0].Position);
        Assert.Equal(GripKind.MoveEntity, grips[0].Kind);
        Assert.Equal(new Point2D(15, 20), grips[1].Position);
        Assert.Equal(new Point2D(10, 25), grips[2].Position);
        Assert.Equal(new Point2D(5, 20), grips[3].Position);
        Assert.Equal(new Point2D(10, 15), grips[4].Position);
    }

    [Fact]
    public void CircleGripProvider_ApplyGripMove_CenterGrip_ShouldMoveCircleAndPreserveRadius()
    {
        var provider = new CircleGripProvider();
        var circle = new CircleEntity(
            new Point2D(10, 20),
            5);

        var result = (CircleEntity)provider.ApplyGripMove(
            circle,
            0,
            new Point2D(30, 40));

        Assert.Equal(circle.Id, result.Id);
        Assert.Equal(new Point2D(30, 40), result.Center);
        Assert.Equal(5, result.Radius);
    }

    [Fact]
    public void CircleGripProvider_ApplyGripMove_QuadrantGrip_ShouldResizeRadius()
    {
        var provider = new CircleGripProvider();
        var circle = new CircleEntity(
            new Point2D(10, 20),
            5);

        var result = (CircleEntity)provider.ApplyGripMove(
            circle,
            1,
            new Point2D(13, 24));

        Assert.Equal(circle.Id, result.Id);
        Assert.Equal(new Point2D(10, 20), result.Center);
        Assert.Equal(5, result.Radius);
    }
}

public sealed class ExtendedGripProviderTests
{
    [Fact]
    public void GripProviderRegistry_ShouldSupportArcsAndPolylines()
    {
        var registry = new GripProviderRegistry();

        Assert.IsType<ArcGripProvider>(registry.FindProvider(new ArcEntity(
            Point2D.Origin,
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90))));

        Assert.IsType<PolylineGripProvider>(registry.FindProvider(new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(0, 5)
        }, isClosed: true)));
    }

    [Fact]
    public void ArcGripProvider_GetGrips_ShouldReturnStartMidEndAndCenter()
    {
        var provider = new ArcGripProvider();
        var arc = new ArcEntity(
            Point2D.Origin,
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        IReadOnlyList<GripPoint> grips = provider.GetGrips(arc);

        Assert.Equal(4, grips.Count);
        AssertPointNear(new Point2D(10, 0), grips[0].Position);
        Assert.Equal(GripKind.MoveVertex, grips[0].Kind);
        AssertPointNear(new Point2D(Math.Sqrt(50), Math.Sqrt(50)), grips[1].Position);
        Assert.Equal(GripKind.ResizeRadius, grips[1].Kind);
        AssertPointNear(new Point2D(0, 10), grips[2].Position);
        Assert.Equal(GripKind.MoveVertex, grips[2].Kind);
        Assert.Equal(Point2D.Origin, grips[3].Position);
        Assert.Equal(GripKind.MoveEntity, grips[3].Kind);
    }

    [Fact]
    public void ArcGripProvider_ApplyGripMove_CenterGrip_ShouldMoveWholeArc()
    {
        var provider = new ArcGripProvider();
        var arc = new ArcEntity(
            Point2D.Origin,
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        var result = (ArcEntity)provider.ApplyGripMove(
            arc,
            3,
            new Point2D(5, 6));

        Assert.Equal(arc.Id, result.Id);
        Assert.Equal(new Point2D(5, 6), result.Center);
        Assert.Equal(10, result.Radius);
        Assert.Equal(arc.StartAngle, result.StartAngle);
        Assert.Equal(arc.EndAngle, result.EndAngle);
    }

    [Fact]
    public void ArcGripProvider_ApplyGripMove_StartGrip_ShouldUpdateStartAngleAndRadius()
    {
        var provider = new ArcGripProvider();
        var arc = new ArcEntity(
            Point2D.Origin,
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        var result = (ArcEntity)provider.ApplyGripMove(
            arc,
            0,
            new Point2D(0, -5));

        Assert.Equal(arc.Id, result.Id);
        Assert.Equal(5, result.Radius);
        AssertNear(-90, result.StartAngle.Degrees);
        AssertNear(90, result.EndAngle.Degrees);
    }

    [Fact]
    public void ArcGripProvider_ApplyGripMove_MidGrip_ShouldResizeRadiusOnly()
    {
        var provider = new ArcGripProvider();
        var arc = new ArcEntity(
            Point2D.Origin,
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        var result = (ArcEntity)provider.ApplyGripMove(
            arc,
            1,
            new Point2D(20, 0));

        Assert.Equal(20, result.Radius);
        Assert.Equal(arc.StartAngle, result.StartAngle);
        Assert.Equal(arc.EndAngle, result.EndAngle);
    }

    [Fact]
    public void PolylineGripProvider_GetGrips_ForGenericPolyline_ShouldReturnVerticesAndCenter()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 3)
        });

        IReadOnlyList<GripPoint> grips = provider.GetGrips(polyline);

        Assert.Equal(6, grips.Count);
        Assert.Equal(new Point2D(0, 0), grips[0].Position);
        Assert.Equal(new Point2D(10, 0), grips[1].Position);
        Assert.Equal(new Point2D(10, 3), grips[2].Position);
        Assert.Equal(new Point2D(5, 0), grips[3].Position);
        Assert.Equal(GripKind.InsertVertex, grips[3].Kind);
        Assert.Equal(new Point2D(10, 1.5), grips[4].Position);
        Assert.Equal(GripKind.InsertVertex, grips[4].Kind);
        Assert.Equal(GripKind.MoveEntity, grips[5].Kind);
    }

    [Fact]
    public void PolylineGripProvider_ApplyGripMove_ForGenericPolylineVertex_ShouldMoveOnlyThatVertex()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 3)
        });

        var result = (PolylineEntity)provider.ApplyGripMove(
            polyline,
            1,
            new Point2D(12, 4));

        Assert.Equal(polyline.Id, result.Id);
        Assert.Equal(new Point2D(0, 0), result.Vertices[0]);
        Assert.Equal(new Point2D(12, 4), result.Vertices[1]);
        Assert.Equal(new Point2D(10, 3), result.Vertices[2]);
    }

    [Fact]
    public void PolylineGripProvider_GetGrips_ForRectangle_ShouldReturnCornersEdgesAndCenter()
    {
        var provider = new PolylineGripProvider();
        var rectangle = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(0, 5)
        }, isClosed: true);

        IReadOnlyList<GripPoint> grips = provider.GetGrips(rectangle);

        Assert.Equal(9, grips.Count);
        Assert.Equal(new Point2D(0, 0), grips[0].Position);
        Assert.Equal(new Point2D(5, 0), grips[1].Position);
        Assert.Equal(new Point2D(10, 0), grips[2].Position);
        Assert.Equal(new Point2D(10, 2.5), grips[3].Position);
        Assert.Equal(new Point2D(10, 5), grips[4].Position);
        Assert.Equal(new Point2D(5, 5), grips[5].Position);
        Assert.Equal(new Point2D(0, 5), grips[6].Position);
        Assert.Equal(new Point2D(0, 2.5), grips[7].Position);
        Assert.Equal(new Point2D(5, 2.5), grips[8].Position);
    }

    [Fact]
    public void PolylineGripProvider_ApplyGripMove_ForRectangleCorner_ShouldPreserveRectangleShape()
    {
        var provider = new PolylineGripProvider();
        var rectangle = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(0, 5)
        }, isClosed: true);

        var result = (PolylineEntity)provider.ApplyGripMove(
            rectangle,
            2,
            new Point2D(14, 8));

        Assert.Equal(new Point2D(0, 0), result.Vertices[0]);
        Assert.Equal(new Point2D(14, 0), result.Vertices[1]);
        Assert.Equal(new Point2D(14, 8), result.Vertices[2]);
        Assert.Equal(new Point2D(0, 8), result.Vertices[3]);
    }

    [Fact]
    public void PolylineGripProvider_ApplyGripMove_ForRectangleEdge_ShouldResizeOneSide()
    {
        var provider = new PolylineGripProvider();
        var rectangle = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(0, 5)
        }, isClosed: true);

        var result = (PolylineEntity)provider.ApplyGripMove(
            rectangle,
            5,
            new Point2D(15, 2));

        Assert.Equal(new Point2D(0, 0), result.Vertices[0]);
        Assert.Equal(new Point2D(15, 0), result.Vertices[1]);
        Assert.Equal(new Point2D(15, 5), result.Vertices[2]);
        Assert.Equal(new Point2D(0, 5), result.Vertices[3]);
    }

    [Fact]
    public void PolylineGripProvider_ApplyGripMove_ForRectangleCenter_ShouldMoveWholeRectangle()
    {
        var provider = new PolylineGripProvider();
        var rectangle = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(0, 5)
        }, isClosed: true);

        var result = (PolylineEntity)provider.ApplyGripMove(
            rectangle,
            8,
            new Point2D(15, 12.5));

        Assert.Equal(new Point2D(10, 10), result.Vertices[0]);
        Assert.Equal(new Point2D(20, 10), result.Vertices[1]);
        Assert.Equal(new Point2D(20, 15), result.Vertices[2]);
        Assert.Equal(new Point2D(10, 15), result.Vertices[3]);
    }

    [Fact]
    public void PolylineGripProvider_ApplyGripMove_ForRotatedRectangle_ShouldPreserveOrientation()
    {
        var provider = new PolylineGripProvider();
        var rectangle = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(0, 5)
        }, isClosed: true);

        var result = (PolylineEntity)provider.ApplyGripMove(
            rectangle,
            5,
            new Point2D(12, 3));

        Assert.Equal(new Point2D(0, 0), result.Vertices[0]);
        Assert.Equal(new Point2D(12, 0), result.Vertices[1]);
        Assert.Equal(new Point2D(12, 5), result.Vertices[2]);
        Assert.Equal(new Point2D(0, 5), result.Vertices[3]);
    }

    private static void AssertPointNear(Point2D expected, Point2D actual)
    {
        AssertNear(expected.X, actual.X);
        AssertNear(expected.Y, actual.Y);
    }

    private static void AssertNear(double expected, double actual)
    {
        Assert.True(
            Math.Abs(expected - actual) < 0.000001,
            $"Expected {expected}, actual {actual}.");
    }
}
