using OpenCad2D.Geometry;

namespace OpenCad2D.Geometry.Primitives;

public readonly record struct Vector2D(double X, double Y)
{
    public double Length => Math.Sqrt(X * X + Y * Y);

    public double LengthSquared => X * X + Y * Y;

    public Vector2D Normalize()
    {
        if (Tolerance.IsZero(Length))
            throw new InvalidOperationException("Cannot normalize a zero-length vector.");

        return new Vector2D(X / Length, Y / Length);
    }

    public double Dot(Vector2D other)
    {
        return X * other.X + Y * other.Y;
    }

    public double Cross(Vector2D other)
    {
        return X * other.Y - Y * other.X;
    }

    public Vector2D PerpendicularLeft()
    {
        return new Vector2D(-Y, X);
    }

    public Vector2D PerpendicularRight()
    {
        return new Vector2D(Y, -X);
    }

    public static Vector2D Zero => new(0, 0);

    public static Vector2D operator +(Vector2D first, Vector2D second)
    {
        return new Vector2D(first.X + second.X, first.Y + second.Y);
    }

    public static Vector2D operator -(Vector2D first, Vector2D second)
    {
        return new Vector2D(first.X - second.X, first.Y - second.Y);
    }

    public static Vector2D operator *(Vector2D vector, double factor)
    {
        return new Vector2D(vector.X * factor, vector.Y * factor);
    }

    public static Vector2D operator /(Vector2D vector, double divisor)
    {
        if (Tolerance.IsZero(divisor))
            throw new DivideByZeroException("Cannot divide a vector by zero.");

        return new Vector2D(vector.X / divisor, vector.Y / divisor);
    }
}