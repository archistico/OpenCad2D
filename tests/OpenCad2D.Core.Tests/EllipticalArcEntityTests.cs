using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class EllipticalArcEntityTests
{
    [Fact]
    public void Constructor_ShouldPreserveNativeEllipseDefinitionAndParameters()
    {
        var arc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI / 2);

        Assert.Equal(EntityKind.EllipticalArc, arc.Kind);
        Assert.Equal(new Point2D(10, 0), arc.StartPoint);
        Assert.True(arc.EndPoint.DistanceTo(new Point2D(0, 5)) <= 1.0e-9);
        Assert.Equal(Math.PI / 2, arc.SweepRadians, precision: 12);
        Assert.True(arc.IsCounterClockwise);
    }

    [Fact]
    public void GetSamplePoints_ShouldFollowDirectedSweepAndIncludeEndpoints()
    {
        var arc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI / 2);

        IReadOnlyList<Point2D> points = arc.GetSamplePoints(4);

        Assert.Equal(5, points.Count);
        Assert.Equal(arc.StartPoint, points[0]);
        Assert.Equal(arc.EndPoint, points[^1]);
        Assert.All(points, point => Assert.True(point.X >= -1.0e-9));
        Assert.All(points, point => Assert.True(point.Y >= -1.0e-9));
    }

    [Fact]
    public void WithLayer_ShouldPreserveGeometry()
    {
        var arc = new EllipticalArcEntity(
            new Point2D(1, 2),
            new Vector2D(10, 0),
            5,
            0.25,
            1.75,
            isCounterClockwise: false);

        var copied = Assert.IsType<EllipticalArcEntity>(arc.WithLayer(new LayerId("Annotations")));

        Assert.Equal(arc.Center, copied.Center);
        Assert.Equal(arc.MajorAxis, copied.MajorAxis);
        Assert.Equal(arc.MinorRadius, copied.MinorRadius);
        Assert.Equal(arc.StartParameterRadians, copied.StartParameterRadians);
        Assert.Equal(arc.EndParameterRadians, copied.EndParameterRadians);
        Assert.Equal(arc.IsCounterClockwise, copied.IsCounterClockwise);
    }
}
