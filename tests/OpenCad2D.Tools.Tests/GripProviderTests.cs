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
