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
        double sweep = NormalizePositiveRadians(end - start);
        double delta = NormalizePositiveRadians(value - start);

        return IsDeltaWithinSweep(delta, sweep, tolerance);
    }

    private static bool IsAngleBetweenClockwise(
        double start,
        double end,
        double value,
        double tolerance)
    {
        double sweep = NormalizePositiveRadians(start - end);
        double delta = NormalizePositiveRadians(start - value);

        return IsDeltaWithinSweep(delta, sweep, tolerance);
    }

    private static bool IsDeltaWithinSweep(
        double delta,
        double sweep,
        double tolerance)
    {
        return delta <= sweep
            || AreAnglesClose(delta, 0.0, tolerance)
            || AreAnglesClose(delta, sweep, tolerance);
    }

    private static bool AreAnglesClose(
        double first,
        double second,
        double tolerance)
    {
        double difference = Math.Abs(first - second);
        double twoPi = 2.0 * Math.PI;

        return difference <= tolerance
            || twoPi - difference <= tolerance;
    }

    private static double NormalizePositiveRadians(double radians)
    {
        double twoPi = 2.0 * Math.PI;
        double value = radians % twoPi;

        if (value < 0)
        {
            value += twoPi;
        }

        return value;
    }
}