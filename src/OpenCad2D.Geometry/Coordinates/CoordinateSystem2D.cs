using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Coordinates;

/// <summary>
/// Represents a two-dimensional user coordinate system mapped to world coordinates.
/// </summary>
public readonly record struct CoordinateSystem2D
{
    public CoordinateSystem2D(
        Point2D origin,
        Vector2D xAxis)
    {
        if (Tolerance.IsZero(xAxis.Length))
        {
            throw new ArgumentException(
                "The X axis cannot be a zero-length vector.",
                nameof(xAxis));
        }

        Origin = origin;
        XAxis = xAxis.Normalize();
        YAxis = XAxis.PerpendicularLeft();
    }

    public Point2D Origin { get; }

    public Vector2D XAxis { get; }

    public Vector2D YAxis { get; }

    public static CoordinateSystem2D World { get; } = new(
        Point2D.Origin,
        new Vector2D(1, 0));

    public static CoordinateSystem2D FromOriginAndAngle(
        Point2D origin,
        double angleRadians)
    {
        return new CoordinateSystem2D(
            origin,
            new Vector2D(
                Math.Cos(angleRadians),
                Math.Sin(angleRadians)));
    }

    public Point2D UserToWorld(Point2D userPoint)
    {
        Vector2D worldOffset =
            XAxis * userPoint.X +
            YAxis * userPoint.Y;

        return Origin + worldOffset;
    }

    public Point2D WorldToUser(Point2D worldPoint)
    {
        Vector2D delta = worldPoint - Origin;

        return new Point2D(
            delta.Dot(XAxis),
            delta.Dot(YAxis));
    }

    public Vector2D UserVectorToWorld(Vector2D userVector)
    {
        return XAxis * userVector.X + YAxis * userVector.Y;
    }

    public Vector2D WorldVectorToUser(Vector2D worldVector)
    {
        return new Vector2D(
            worldVector.Dot(XAxis),
            worldVector.Dot(YAxis));
    }
}