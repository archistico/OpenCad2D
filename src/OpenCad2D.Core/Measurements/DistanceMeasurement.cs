using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Measurements;

/// <summary>
/// Result of a distance measurement between two model-space points.
/// </summary>
public sealed class DistanceMeasurement
{
    public DistanceMeasurement(Point2D firstPoint, Point2D secondPoint)
    {
        FirstPoint = firstPoint;
        SecondPoint = secondPoint;

        DeltaX = secondPoint.X - firstPoint.X;
        DeltaY = secondPoint.Y - firstPoint.Y;
        Distance = firstPoint.DistanceTo(secondPoint);
        AngleDegrees = NormalizeDegrees(Math.Atan2(DeltaY, DeltaX) * 180.0 / Math.PI);
    }

    public Point2D FirstPoint { get; }

    public Point2D SecondPoint { get; }

    public double DeltaX { get; }

    public double DeltaY { get; }

    public double Distance { get; }

    public double AngleDegrees { get; }

    private static double NormalizeDegrees(double degrees)
    {
        double value = degrees % 360.0;

        if (value < 0)
        {
            value += 360.0;
        }

        return value;
    }
}
