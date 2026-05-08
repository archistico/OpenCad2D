namespace OpenCad2D.Geometry.Primitives;

/// <summary>
/// Represents a circular arc.
/// Angles are stored in radians.
/// The default direction is counter-clockwise.
/// </summary>
public readonly record struct Arc2D
{
    public Arc2D(
        Point2D center,
        double radius,
        Angle startAngle,
        Angle endAngle,
        bool isCounterClockwise = true)
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Arc radius must be greater than zero.");
        }

        Center = center;
        Radius = radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
        IsCounterClockwise = isCounterClockwise;
    }

    public Point2D Center { get; }

    public double Radius { get; }

    public Angle StartAngle { get; }

    public Angle EndAngle { get; }

    public bool IsCounterClockwise { get; }

    public Point2D StartPoint => PointAt(StartAngle);

    public Point2D EndPoint => PointAt(EndAngle);

    public Point2D PointAt(Angle angle)
    {
        return new Point2D(
            Center.X + Math.Cos(angle.Radians) * Radius,
            Center.Y + Math.Sin(angle.Radians) * Radius);
    }

    public bool ContainsAngle(Angle angle, double tolerance = Tolerance.Default)
    {
        double start = StartAngle.NormalizePositive().Radians;
        double end = EndAngle.NormalizePositive().Radians;
        double value = angle.NormalizePositive().Radians;

        if (IsCounterClockwise)
        {
            return IsAngleBetweenCounterClockwise(start, end, value, tolerance);
        }

        return IsAngleBetweenClockwise(start, end, value, tolerance);
    }

    public bool ContainsPoint(Point2D point, double tolerance = Tolerance.Default)
    {
        double distance = Center.DistanceTo(point);

        if (!Tolerance.AreEqual(distance, Radius, tolerance))
        {
            return false;
        }

        double radians = Math.Atan2(
            point.Y - Center.Y,
            point.X - Center.X);

        return ContainsAngle(Angle.FromRadians(radians), tolerance);
    }

    public BoundingBox2D GetBoundingBox()
    {
        var points = new List<Point2D>
        {
            StartPoint,
            EndPoint
        };

        var cardinalAngles = new[]
        {
            Angle.FromDegrees(0),
            Angle.FromDegrees(90),
            Angle.FromDegrees(180),
            Angle.FromDegrees(270)
        };

        foreach (Angle cardinalAngle in cardinalAngles)
        {
            if (ContainsAngle(cardinalAngle))
            {
                points.Add(PointAt(cardinalAngle));
            }
        }

        double minX = points.Min(point => point.X);
        double minY = points.Min(point => point.Y);
        double maxX = points.Max(point => point.X);
        double maxY = points.Max(point => point.Y);

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    private static bool IsAngleBetweenCounterClockwise(
        double start,
        double end,
        double value,
        double tolerance)
    {
        if (end < start)
        {
            end += 2.0 * Math.PI;
        }

        if (value < start)
        {
            value += 2.0 * Math.PI;
        }

        return value >= start - tolerance
            && value <= end + tolerance;
    }

    private static bool IsAngleBetweenClockwise(
        double start,
        double end,
        double value,
        double tolerance)
    {
        if (start < end)
        {
            start += 2.0 * Math.PI;
        }

        if (value > start)
        {
            value -= 2.0 * Math.PI;
        }

        return value <= start + tolerance
            && value >= end - tolerance;
    }
}