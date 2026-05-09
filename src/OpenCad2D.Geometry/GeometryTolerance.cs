using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry;

/// <summary>
/// Defines the numeric tolerance strategy used by geometric algorithms.
/// </summary>
public readonly record struct GeometryTolerance
{
    public GeometryTolerance(
        double distance,
        double angle,
        double parameter,
        double vectorLength)
    {
        if (distance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distance),
                "Distance tolerance cannot be negative.");
        }

        if (angle < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(angle),
                "Angle tolerance cannot be negative.");
        }

        if (parameter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameter),
                "Parameter tolerance cannot be negative.");
        }

        if (vectorLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vectorLength),
                "Vector length tolerance cannot be negative.");
        }

        Distance = distance;
        Angle = angle;
        Parameter = parameter;
        VectorLength = vectorLength;
    }

    /// <summary>
    /// Tolerance used for comparing points, coordinates and distances.
    /// </summary>
    public double Distance { get; }

    /// <summary>
    /// Tolerance used for comparing angles, expressed in radians.
    /// </summary>
    public double Angle { get; }

    /// <summary>
    /// Tolerance used for normalized parameters, for example segment parameters in [0, 1].
    /// </summary>
    public double Parameter { get; }

    /// <summary>
    /// Tolerance used for detecting zero-length vectors.
    /// </summary>
    public double VectorLength { get; }

    public static GeometryTolerance Default { get; } = new(
        distance: 1e-9,
        angle: 1e-10,
        parameter: 1e-12,
        vectorLength: 1e-12);

    public bool AreDistancesEqual(double first, double second)
    {
        return Math.Abs(first - second) <= Distance;
    }

    public bool AreAnglesEqual(double firstRadians, double secondRadians)
    {
        return Math.Abs(firstRadians - secondRadians) <= Angle;
    }

    public bool AreParametersEqual(double first, double second)
    {
        return Math.Abs(first - second) <= Parameter;
    }

    public bool IsDistanceZero(double value)
    {
        return Math.Abs(value) <= Distance;
    }

    public bool IsVectorLengthZero(double value)
    {
        return Math.Abs(value) <= VectorLength;
    }

    public bool IsParameterWithinUnitInterval(double value)
    {
        return value >= -Parameter && value <= 1.0 + Parameter;
    }

    public bool ArePointsEqual(Point2D first, Point2D second)
    {
        return first.DistanceTo(second) <= Distance;
    }

    public bool AreCoordinatesEqual(Point2D first, Point2D second)
    {
        return AreDistancesEqual(first.X, second.X)
            && AreDistancesEqual(first.Y, second.Y);
    }
}