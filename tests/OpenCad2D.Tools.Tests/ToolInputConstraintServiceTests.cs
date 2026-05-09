using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Tests;

public sealed class ToolInputConstraintServiceTests
{
    [Fact]
    public void ApplyOrtho_WhenDisabled_ShouldReturnCurrentPoint()
    {
        Point2D result = ToolInputConstraintService.ApplyOrtho(
            isOrthoEnabled: false,
            new Point2D(100, 100),
            new Point2D(180, 120));

        Assert.Equal(new Point2D(180, 120), result);
    }

    [Fact]
    public void ApplyOrtho_WhenHorizontalDeltaIsGreater_ShouldLockY()
    {
        Point2D result = ToolInputConstraintService.ApplyOrtho(
            isOrthoEnabled: true,
            new Point2D(100, 100),
            new Point2D(180, 120));

        Assert.Equal(new Point2D(180, 100), result);
    }

    [Fact]
    public void ApplyOrtho_WhenVerticalDeltaIsGreater_ShouldLockX()
    {
        Point2D result = ToolInputConstraintService.ApplyOrtho(
            isOrthoEnabled: true,
            new Point2D(100, 100),
            new Point2D(120, 180));

        Assert.Equal(new Point2D(100, 180), result);
    }

    [Fact]
    public void ApplyOrtho_WhenDeltasAreEqual_ShouldPreferHorizontalDirection()
    {
        Point2D result = ToolInputConstraintService.ApplyOrtho(
            isOrthoEnabled: true,
            new Point2D(100, 100),
            new Point2D(150, 150));

        Assert.Equal(new Point2D(150, 100), result);
    }
}
