namespace OpenCad2D.Geometry.Primitives;

public readonly record struct Point2D(double X, double Y)
{
    public double DistanceTo(Point2D other)
    {
        double dx = other.X - X;
        double dy = other.Y - Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }

    public Vector2D VectorTo(Point2D other)
    {
        return new Vector2D(other.X - X, other.Y - Y);
    }

    public Point2D Translate(Vector2D vector)
    {
        return new Point2D(X + vector.X, Y + vector.Y);
    }

    public static Point2D Origin => new(0, 0);

    public static Vector2D operator -(Point2D end, Point2D start)
    {
        return new Vector2D(end.X - start.X, end.Y - start.Y);
    }

    public static Point2D operator +(Point2D point, Vector2D vector)
    {
        return new Point2D(point.X + vector.X, point.Y + vector.Y);
    }

    public static Point2D operator -(Point2D point, Vector2D vector)
    {
        return new Point2D(point.X - vector.X, point.Y - vector.Y);
    }
}