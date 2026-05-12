using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Dimensions.Rendering;

/// <summary>
/// Represents one circular arc used to render a dimension entity.
/// </summary>
public readonly record struct DimensionArcPrimitive(
    Point2D Center,
    double Radius,
    double StartAngleDegrees,
    double EndAngleDegrees,
    bool IsCounterClockwise)
{
    public Point2D StartPoint => PointAt(StartAngleDegrees);

    public Point2D EndPoint => PointAt(EndAngleDegrees);

    public Arc2D ToArc2D()
    {
        return new Arc2D(
            Center,
            Radius,
            Angle.FromDegrees(StartAngleDegrees),
            Angle.FromDegrees(EndAngleDegrees),
            IsCounterClockwise);
    }

    public Point2D PointAt(double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;

        return new Point2D(
            Center.X + Math.Cos(radians) * Radius,
            Center.Y + Math.Sin(radians) * Radius);
    }
}
