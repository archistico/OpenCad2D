using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Measurements;

/// <summary>
/// Result of an angle measurement defined by two rays sharing a vertex.
/// </summary>
public sealed class AngleMeasurement
{
    public AngleMeasurement(
        Point2D firstRayPoint,
        Point2D vertex,
        Point2D secondRayPoint)
    {
        FirstRayPoint = firstRayPoint;
        Vertex = vertex;
        SecondRayPoint = secondRayPoint;

        double firstAngle = Math.Atan2(
            firstRayPoint.Y - vertex.Y,
            firstRayPoint.X - vertex.X);

        double secondAngle = Math.Atan2(
            secondRayPoint.Y - vertex.Y,
            secondRayPoint.X - vertex.X);

        double delta = Math.Abs(secondAngle - firstAngle) * 180.0 / Math.PI;

        if (delta > 180.0)
        {
            delta = 360.0 - delta;
        }

        Degrees = delta;
        SupplementaryDegrees = 180.0 - delta;
    }

    public Point2D FirstRayPoint { get; }

    public Point2D Vertex { get; }

    public Point2D SecondRayPoint { get; }

    /// <summary>
    /// Smaller angle between the two rays, expressed in degrees.
    /// </summary>
    public double Degrees { get; }

    /// <summary>
    /// Supplementary angle, expressed in degrees.
    /// </summary>
    public double SupplementaryDegrees { get; }
}
