namespace OpenCad2D.Geometry.Primitives;

public readonly record struct BoundingBox2D(
    double MinX,
    double MinY,
    double MaxX,
    double MaxY)
{
    public double Width => MaxX - MinX;

    public double Height => MaxY - MinY;

    public Point2D Center => new(
        (MinX + MaxX) / 2.0,
        (MinY + MaxY) / 2.0);

    public bool Contains(Point2D point)
    {
        return point.X >= MinX
            && point.X <= MaxX
            && point.Y >= MinY
            && point.Y <= MaxY;
    }

    public bool Contains(BoundingBox2D other)
    {
        return other.MinX >= MinX
            && other.MaxX <= MaxX
            && other.MinY >= MinY
            && other.MaxY <= MaxY;
    }

    public bool Intersects(BoundingBox2D other)
    {
        return MinX <= other.MaxX
            && MaxX >= other.MinX
            && MinY <= other.MaxY
            && MaxY >= other.MinY;
    }

    public BoundingBox2D Expand(double amount)
    {
        return new BoundingBox2D(
            MinX - amount,
            MinY - amount,
            MaxX + amount,
            MaxY + amount);
    }

    public static BoundingBox2D FromPoints(Point2D first, Point2D second)
    {
        return new BoundingBox2D(
            Math.Min(first.X, second.X),
            Math.Min(first.Y, second.Y),
            Math.Max(first.X, second.X),
            Math.Max(first.Y, second.Y));
    }
}