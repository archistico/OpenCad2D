using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Transformations;

public readonly record struct Matrix2D(
    double M11,
    double M12,
    double M21,
    double M22,
    double OffsetX,
    double OffsetY)
{
    public static Matrix2D Identity => new(
        1, 0,
        0, 1,
        0, 0);

    public static Matrix2D Translation(double x, double y)
    {
        return new Matrix2D(
            1, 0,
            0, 1,
            x, y);
    }

    public static Matrix2D Scale(double factor, Point2D center)
    {
        return new Matrix2D(
            factor, 0,
            0, factor,
            center.X - factor * center.X,
            center.Y - factor * center.Y);
    }

    public static Matrix2D Rotation(double radians, Point2D center)
    {
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        double offsetX = center.X - cos * center.X + sin * center.Y;
        double offsetY = center.Y - sin * center.X - cos * center.Y;

        return new Matrix2D(
            cos, -sin,
            sin, cos,
            offsetX, offsetY);
    }

    public Point2D Transform(Point2D point)
    {
        double x = point.X * M11 + point.Y * M12 + OffsetX;
        double y = point.X * M21 + point.Y * M22 + OffsetY;

        return new Point2D(x, y);
    }

    public Vector2D Transform(Vector2D vector)
    {
        double x = vector.X * M11 + vector.Y * M12;
        double y = vector.X * M21 + vector.Y * M22;

        return new Vector2D(x, y);
    }

    public Matrix2D Multiply(Matrix2D other)
    {
        return new Matrix2D(
            M11 * other.M11 + M12 * other.M21,
            M11 * other.M12 + M12 * other.M22,
            M21 * other.M11 + M22 * other.M21,
            M21 * other.M12 + M22 * other.M22,
            OffsetX * other.M11 + OffsetY * other.M21 + other.OffsetX,
            OffsetX * other.M12 + OffsetY * other.M22 + other.OffsetY);
    }
}