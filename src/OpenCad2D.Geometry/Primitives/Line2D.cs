using OpenCad2D.Geometry;

namespace OpenCad2D.Geometry.Primitives;

/// <summary>
/// Represents an infinite 2D line defined by one point and one direction.
/// </summary>
public readonly record struct Line2D(Point2D Point, Vector2D Direction)
{
    public Vector2D NormalizedDirection
    {
        get
        {
            if (Tolerance.IsZero(Direction.Length))
                throw new InvalidOperationException("A line cannot have a zero-length direction.");

            return Direction.Normalize();
        }
    }

    public static Line2D FromPoints(Point2D first, Point2D second)
    {
        Vector2D direction = first.VectorTo(second);

        if (Tolerance.IsZero(direction.Length))
            throw new InvalidOperationException("Cannot create a line from two identical points.");

        return new Line2D(first, direction);
    }
}