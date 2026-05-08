namespace OpenCad2D.Geometry;

public static class Tolerance
{
    public const double Default = 1e-9;

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
}