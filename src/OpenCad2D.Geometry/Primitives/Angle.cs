namespace OpenCad2D.Geometry.Primitives;

/// <summary>
/// Represents an angle stored internally in radians.
/// </summary>
public readonly record struct Angle(double Radians)
{
    public double Degrees => Radians * 180.0 / Math.PI;

    public static Angle FromRadians(double radians)
    {
        return new Angle(radians);
    }

    public static Angle FromDegrees(double degrees)
    {
        return new Angle(degrees * Math.PI / 180.0);
    }

    public Angle NormalizePositive()
    {
        double value = Radians % (2.0 * Math.PI);

        if (value < 0)
            value += 2.0 * Math.PI;

        return new Angle(value);
    }

    public static Angle Zero => new(0);

    public static Angle FullCircle => new(2.0 * Math.PI);
}