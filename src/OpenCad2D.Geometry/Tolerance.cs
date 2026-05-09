using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry;

/// <summary>
/// Compatibility helper for numeric tolerance checks.
/// Prefer GeometryTolerance in new geometric algorithms.
/// </summary>
public static class Tolerance
{
    public const double Default = 1e-9;

    public static GeometryTolerance DefaultGeometry { get; } =
        GeometryTolerance.Default;

    public static bool AreEqual(
        double first,
        double second,
        double tolerance = Default)
    {
        return Math.Abs(first - second) <= tolerance;
    }

    public static bool IsZero(
        double value,
        double tolerance = Default)
    {
        return Math.Abs(value) <= tolerance;
    }

    public static bool ArePointsEqual(
        Point2D first,
        Point2D second,
        double tolerance = Default)
    {
        return first.DistanceTo(second) <= tolerance;
    }

    public static bool IsWithinUnitInterval(
        double value,
        double tolerance = Default)
    {
        return value >= -tolerance && value <= 1.0 + tolerance;
    }
}