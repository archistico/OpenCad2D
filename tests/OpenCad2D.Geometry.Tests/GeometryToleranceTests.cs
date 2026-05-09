using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class GeometryToleranceTests
{
    [Fact]
    public void ArePointsEqual_WhenDistanceIsWithinTolerance_ShouldReturnTrue()
    {
        GeometryTolerance tolerance = new(
            distance: 0.01,
            angle: 1e-10,
            parameter: 1e-12,
            vectorLength: 1e-12);

        bool result = tolerance.ArePointsEqual(
            new Point2D(0, 0),
            new Point2D(0.005, 0.005));

        Assert.True(result);
    }

    [Fact]
    public void ArePointsEqual_WhenDistanceIsOutsideTolerance_ShouldReturnFalse()
    {
        GeometryTolerance tolerance = new(
            distance: 0.01,
            angle: 1e-10,
            parameter: 1e-12,
            vectorLength: 1e-12);

        bool result = tolerance.ArePointsEqual(
            new Point2D(0, 0),
            new Point2D(0.02, 0));

        Assert.False(result);
    }

    [Fact]
    public void IsParameterWithinUnitInterval_ShouldAcceptSmallOvershoot()
    {
        GeometryTolerance tolerance = new(
            distance: 1e-9,
            angle: 1e-10,
            parameter: 0.001,
            vectorLength: 1e-12);

        Assert.True(tolerance.IsParameterWithinUnitInterval(-0.0005));
        Assert.True(tolerance.IsParameterWithinUnitInterval(1.0005));
    }

    [Fact]
    public void IsParameterWithinUnitInterval_ShouldRejectLargeOvershoot()
    {
        GeometryTolerance tolerance = new(
            distance: 1e-9,
            angle: 1e-10,
            parameter: 0.001,
            vectorLength: 1e-12);

        Assert.False(tolerance.IsParameterWithinUnitInterval(-0.01));
        Assert.False(tolerance.IsParameterWithinUnitInterval(1.01));
    }
}