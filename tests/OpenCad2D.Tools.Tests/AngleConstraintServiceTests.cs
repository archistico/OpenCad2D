using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Tests;

public sealed class AngleConstraintServiceTests
{
    [Fact]
    public void Apply_WhenDisabled_ShouldReturnCandidatePoint()
    {
        Point2D candidate = new(10, 3);

        Point2D result = AngleConstraintService.Apply(
            Point2D.Origin,
            candidate,
            AngleConstraintSettings.Off);

        Assert.Equal(candidate, result);
    }

    [Fact]
    public void Apply_WhenCandidateEqualsBasePoint_ShouldReturnCandidatePoint()
    {
        Point2D point = new(5, 7);

        Point2D result = AngleConstraintService.Apply(
            point,
            point,
            AngleConstraintSettings.FromStep(45));

        Assert.Equal(point, result);
    }

    [Fact]
    public void Apply_WithFortyFiveDegrees_ShouldConstrainToNearestDiagonal()
    {
        Point2D result = AngleConstraintService.Apply(
            Point2D.Origin,
            PointFromPolar(10, 38),
            AngleConstraintSettings.FromStep(45));

        AssertPointNear(PointFromPolar(10, 45), result);
    }

    [Fact]
    public void Apply_WithFortyFiveDegrees_ShouldConstrainToHorizontalDirection()
    {
        Point2D result = AngleConstraintService.Apply(
            Point2D.Origin,
            PointFromPolar(10, 12),
            AngleConstraintSettings.FromStep(45));

        AssertPointNear(new Point2D(10, 0), result);
    }

    [Fact]
    public void Apply_WithFortyFiveDegrees_ShouldConstrainToVerticalDirection()
    {
        Point2D result = AngleConstraintService.Apply(
            Point2D.Origin,
            PointFromPolar(10, 78),
            AngleConstraintSettings.FromStep(45));

        AssertPointNear(new Point2D(0, 10), result);
    }

    [Fact]
    public void Apply_WithNegativeAngle_ShouldConstrainToNearestDirection()
    {
        Point2D result = AngleConstraintService.Apply(
            Point2D.Origin,
            PointFromPolar(10, -30),
            AngleConstraintSettings.FromStep(45));

        AssertPointNear(PointFromPolar(10, -45), result);
    }

    [Fact]
    public void Apply_ShouldPreserveDistanceFromBasePoint()
    {
        Point2D basePoint = new(100, 50);
        Point2D candidate = new(113, 61);

        Point2D result = AngleConstraintService.Apply(
            basePoint,
            candidate,
            AngleConstraintSettings.FromStep(30));

        Assert.Equal(
            basePoint.DistanceTo(candidate),
            basePoint.DistanceTo(result),
            precision: 10);
    }

    [Theory]
    [InlineData(38, 45, 45)]
    [InlineData(12, 45, 0)]
    [InlineData(78, 45, 90)]
    [InlineData(181, 90, 180)]
    [InlineData(-20, 45, 0)]
    [InlineData(-30, 45, -45)]
    public void GetNearestAngleDegrees_ShouldRoundToNearestStep(
        double sourceAngle,
        double step,
        double expectedAngle)
    {
        double result = AngleConstraintService.GetNearestAngleDegrees(
            sourceAngle,
            step);

        Assert.Equal(expectedAngle, result, precision: 10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-45)]
    [InlineData(181)]
    [InlineData(double.PositiveInfinity)]
    public void FromStep_WithInvalidStep_ShouldThrow(double stepDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AngleConstraintSettings.FromStep(stepDegrees));
    }

    private static Point2D PointFromPolar(
        double distance,
        double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;

        return new Point2D(
            Math.Cos(radians) * distance,
            Math.Sin(radians) * distance);
    }

    private static void AssertPointNear(
        Point2D expected,
        Point2D actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 10);
        Assert.Equal(expected.Y, actual.Y, precision: 10);
    }
}
