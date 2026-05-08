namespace OpenCad2D.Geometry.Primitives;

/// <summary>
/// Represents a 2D circle.
/// </summary>
public readonly record struct Circle2D
{
    public Circle2D(Point2D center, double radius)
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Circle radius must be greater than zero.");
        }

        Center = center;
        Radius = radius;
    }

    public Point2D Center { get; }

    public double Radius { get; }

    public BoundingBox2D GetBoundingBox()
    {
        return new BoundingBox2D(
            Center.X - Radius,
            Center.Y - Radius,
            Center.X + Radius,
            Center.Y + Radius);
    }

    public Point2D PointAt(Angle angle)
    {
        return new Point2D(
            Center.X + Math.Cos(angle.Radians) * Radius,
            Center.Y + Math.Sin(angle.Radians) * Radius);
    }

    public bool Contains(Point2D point, double tolerance = Tolerance.Default)
    {
        double distance = Center.DistanceTo(point);

        return Tolerance.AreEqual(distance, Radius, tolerance);
    }
}