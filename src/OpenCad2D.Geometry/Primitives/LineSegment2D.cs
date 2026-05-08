namespace OpenCad2D.Geometry.Primitives;

public readonly record struct LineSegment2D(Point2D Start, Point2D End)
{
    public double Length => Start.DistanceTo(End);

    public Point2D Midpoint => new(
        (Start.X + End.X) / 2.0,
        (Start.Y + End.Y) / 2.0);

    public Vector2D Direction => Start.VectorTo(End);

    public BoundingBox2D GetBoundingBox()
    {
        return BoundingBox2D.FromPoints(Start, End);
    }
}