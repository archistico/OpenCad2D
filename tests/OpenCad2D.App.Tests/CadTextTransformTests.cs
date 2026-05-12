using Avalonia;
using OpenCad2D.App.Controls;

namespace OpenCad2D.App.Tests;

public sealed class CadTextTransformTests
{
    [Fact]
    public void CreateCadRotationAt_WithPositiveNinetyDegrees_ShouldRotateClockwiseOnScreen()
    {
        Matrix matrix = CadTextTransform.CreateCadRotationAt(
            90.0,
            10.0,
            20.0);

        Point transformed = Transform(
            matrix,
            new Point(15.0, 20.0));

        AssertClose(10.0, transformed.X);
        AssertClose(25.0, transformed.Y);
    }

    [Fact]
    public void CreateCadRotationAt_WithNegativeNinetyDegrees_ShouldRotateCounterClockwiseOnScreen()
    {
        Matrix matrix = CadTextTransform.CreateCadRotationAt(
            -90.0,
            10.0,
            20.0);

        Point transformed = Transform(
            matrix,
            new Point(15.0, 20.0));

        AssertClose(10.0, transformed.X);
        AssertClose(15.0, transformed.Y);
    }

    [Fact]
    public void CreateCadRotationAt_ShouldKeepRotationCenterFixed()
    {
        Matrix matrix = CadTextTransform.CreateCadRotationAt(
            37.0,
            10.0,
            20.0);

        Point transformed = Transform(
            matrix,
            new Point(10.0, 20.0));

        AssertClose(10.0, transformed.X);
        AssertClose(20.0, transformed.Y);
    }

    private static Point Transform(
        Matrix matrix,
        Point point)
    {
        return new Point(
            (point.X * matrix.M11) + (point.Y * matrix.M21) + matrix.M31,
            (point.X * matrix.M12) + (point.Y * matrix.M22) + matrix.M32);
    }

    private static void AssertClose(
        double expected,
        double actual)
    {
        Assert.True(
            Math.Abs(expected - actual) < 0.000001,
            $"Expected {expected}, actual {actual}.");
    }
}
