using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Measurements;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class MeasurementServiceTests
{
    [Fact]
    public void MeasureDistance_ShouldReturnDistanceDeltaAndAngle()
    {
        DistanceMeasurement measurement = MeasurementService.MeasureDistance(
            new Point2D(1, 2),
            new Point2D(4, 6));

        Assert.Equal(3, measurement.DeltaX, 6);
        Assert.Equal(4, measurement.DeltaY, 6);
        Assert.Equal(5, measurement.Distance, 6);
        Assert.Equal(53.130102, measurement.AngleDegrees, 6);
    }

    [Fact]
    public void MeasureDistance_WhenVectorPointsDown_ShouldNormalizeAnglePositive()
    {
        DistanceMeasurement measurement = MeasurementService.MeasureDistance(
            Point2D.Origin,
            new Point2D(0, -10));

        Assert.Equal(270, measurement.AngleDegrees, 6);
    }

    [Fact]
    public void MeasureAngle_ShouldReturnSmallerAngleBetweenThreePoints()
    {
        AngleMeasurement measurement = MeasurementService.MeasureAngle(
            new Point2D(10, 0),
            Point2D.Origin,
            new Point2D(0, 10));

        Assert.Equal(90, measurement.Degrees, 6);
        Assert.Equal(90, measurement.SupplementaryDegrees, 6);
    }

    [Fact]
    public void MeasureAngle_WhenReflexDifferenceExists_ShouldReturnSmallerAngle()
    {
        AngleMeasurement measurement = MeasurementService.MeasureAngle(
            new Point2D(-1, 0),
            Point2D.Origin,
            new Point2D(0, -1));

        Assert.Equal(90, measurement.Degrees, 6);
    }

    [Fact]
    public void MeasureEntity_WithPoint_ShouldReturnPointKindWithoutLinearValues()
    {
        var point = new PointEntity(new Point2D(2, 3));

        EntityMeasurement measurement = MeasurementService.MeasureEntity(point);

        Assert.Equal(EntityKind.Point, measurement.EntityKind);
        Assert.Null(measurement.Length);
        Assert.Null(measurement.Area);
        Assert.Equal("Point", MeasurementFormatter.FormatEntity(measurement));
    }

    [Fact]
    public void MeasureEntity_WithLine_ShouldReturnLengthAndAngle()
    {
        var line = new LineEntity(
            Point2D.Origin,
            new Point2D(10, 0));

        EntityMeasurement measurement = MeasurementService.MeasureEntity(line);

        Assert.Equal(EntityKind.Line, measurement.EntityKind);
        Assert.Equal(10, measurement.Length!.Value);
        Assert.Equal(0, measurement.AngleDegrees!.Value);
        Assert.Null(measurement.Area);
    }

    [Fact]
    public void MeasureEntity_WithCircle_ShouldReturnRadiusDiameterAreaAndCircumference()
    {
        var circle = new CircleEntity(Point2D.Origin, 5);

        EntityMeasurement measurement = MeasurementService.MeasureEntity(circle);

        Assert.Equal(EntityKind.Circle, measurement.EntityKind);
        Assert.Equal(5, measurement.Radius);
        Assert.Equal(10, measurement.Diameter);
        Assert.Equal(2.0 * Math.PI * 5, measurement.Circumference!.Value, 6);
        Assert.Equal(Math.PI * 25, measurement.Area!.Value, 6);
    }

    [Fact]
    public void MeasureEntity_WithCounterClockwiseArc_ShouldReturnSweepAndLength()
    {
        var arc = new ArcEntity(
            Point2D.Origin,
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        EntityMeasurement measurement = MeasurementService.MeasureEntity(arc);

        Assert.Equal(EntityKind.Arc, measurement.EntityKind);
        Assert.Equal(10, measurement.Radius);
        Assert.Equal(90, measurement.SweepAngleDegrees!.Value, 6);
        Assert.Equal(Math.PI * 5, measurement.Length!.Value, 6);
    }

    [Fact]
    public void MeasureEntity_WithClockwiseArc_ShouldReturnPositiveSweepAndLength()
    {
        var arc = new ArcEntity(
            Point2D.Origin,
            10,
            Angle.FromDegrees(90),
            Angle.FromDegrees(0),
            isCounterClockwise: false);

        EntityMeasurement measurement = MeasurementService.MeasureEntity(arc);

        Assert.Equal(90, measurement.SweepAngleDegrees!.Value, 6);
        Assert.Equal(Math.PI * 5, measurement.Length!.Value, 6);
    }

    [Fact]
    public void MeasureEntity_WithOpenPolyline_ShouldReturnLengthAndNoArea()
    {
        var polyline = new PolylineEntity(new[]
        {
            Point2D.Origin,
            new Point2D(3, 4),
            new Point2D(6, 4),
        });

        EntityMeasurement measurement = MeasurementService.MeasureEntity(polyline);

        Assert.Equal(EntityKind.Polyline, measurement.EntityKind);
        Assert.Equal(8, measurement.Length!.Value);
        Assert.Null(measurement.Area);
        Assert.Equal(3, measurement.VertexCount!.Value);
        Assert.False(measurement.IsClosed!.Value);
    }

    [Fact]
    public void MeasureEntity_WithClosedPolyline_ShouldReturnPerimeterAndArea()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                Point2D.Origin,
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5),
            },
            isClosed: true);

        EntityMeasurement measurement = MeasurementService.MeasureEntity(polyline);

        Assert.Equal(30, measurement.Length!.Value);
        Assert.Equal(50, measurement.Area!.Value);
        Assert.True(measurement.IsClosed!.Value);
    }

    [Fact]
    public void CalculatePolygonArea_ShouldBePositiveRegardlessOfWinding()
    {
        var clockwise = new[]
        {
            Point2D.Origin,
            new Point2D(0, 5),
            new Point2D(10, 5),
            new Point2D(10, 0),
        };

        double area = MeasurementService.CalculatePolygonArea(clockwise);

        Assert.Equal(50, area);
    }

    [Fact]
    public void FormatDistance_ShouldUseInvariantCompactOutput()
    {
        DistanceMeasurement measurement = MeasurementService.MeasureDistance(
            Point2D.Origin,
            new Point2D(3, 4));

        string text = MeasurementFormatter.FormatDistance(measurement);

        Assert.Equal("Distance: 5 | ΔX: 3 | ΔY: 4 | Angle: 53.13°", text);
    }
}
