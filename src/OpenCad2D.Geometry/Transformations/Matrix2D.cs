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

    public Matrix2D Invert()
    {
        double determinant = M11 * M22 - M12 * M21;

        if (Math.Abs(determinant) <= 1e-12)
        {
            throw new InvalidOperationException("Matrix cannot be inverted because it is singular.");
        }

        double inv11 = M22 / determinant;
        double inv12 = -M12 / determinant;
        double inv21 = -M21 / determinant;
        double inv22 = M11 / determinant;
        double invOffsetX = -(OffsetX * inv11 + OffsetY * inv12);
        double invOffsetY = -(OffsetX * inv21 + OffsetY * inv22);

        return new Matrix2D(
            inv11,
            inv12,
            inv21,
            inv22,
            invOffsetX,
            invOffsetY);
    }

    public static Matrix2D Mirror(Line2D mirrorLine)
    {
        Vector2D direction = mirrorLine.NormalizedDirection;

        double ux = direction.X;
        double uy = direction.Y;

        double m11 = 2 * ux * ux - 1;
        double m12 = 2 * ux * uy;
        double m21 = 2 * ux * uy;
        double m22 = 2 * uy * uy - 1;

        double x0 = mirrorLine.Point.X;
        double y0 = mirrorLine.Point.Y;

        double offsetX = x0 - (m11 * x0 + m12 * y0);
        double offsetY = y0 - (m21 * x0 + m22 * y0);

        return new Matrix2D(
            m11,
            m12,
            m21,
            m22,
            offsetX,
            offsetY);
    }
}