using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Transformations;

/// <summary>
/// Computes transformation matrices for CAD align operations.
/// </summary>
public sealed class AlignTransformService
{
    /// <summary>
    /// Computes a transformation that maps <paramref name="sourcePoint1" /> to
    /// <paramref name="destinationPoint1" /> and rotates the source direction
    /// toward the destination direction. Uniform scaling can be applied optionally.
    /// </summary>
    public AlignTransformResult Calculate(
        Point2D sourcePoint1,
        Point2D destinationPoint1,
        Point2D sourcePoint2,
        Point2D destinationPoint2,
        bool applyScale)
    {
        Vector2D sourceVector = sourcePoint1.VectorTo(sourcePoint2);
        Vector2D destinationVector = destinationPoint1.VectorTo(destinationPoint2);

        if (Tolerance.IsZero(sourceVector.Length) ||
            Tolerance.IsZero(destinationVector.Length))
        {
            Matrix2D translation = Matrix2D.Translation(
                destinationPoint1.X - sourcePoint1.X,
                destinationPoint1.Y - sourcePoint1.Y);

            return new AlignTransformResult(
                translation,
                sourcePoint1,
                destinationPoint1,
                sourcePoint2,
                destinationPoint2,
                rotationRadians: 0,
                scaleFactor: 1,
                scaleApplied: false,
                isDegenerate: true);
        }

        double sourceAngle = Math.Atan2(
            sourceVector.Y,
            sourceVector.X);

        double destinationAngle = Math.Atan2(
            destinationVector.Y,
            destinationVector.X);

        double rotationRadians = destinationAngle - sourceAngle;
        double scaleFactor = applyScale
            ? destinationVector.Length / sourceVector.Length
            : 1.0;

        double cos = Math.Cos(rotationRadians) * scaleFactor;
        double sin = Math.Sin(rotationRadians) * scaleFactor;

        double offsetX = destinationPoint1.X -
                         (sourcePoint1.X * cos + sourcePoint1.Y * -sin);
        double offsetY = destinationPoint1.Y -
                         (sourcePoint1.X * sin + sourcePoint1.Y * cos);

        Matrix2D matrix = new(
            cos,
            -sin,
            sin,
            cos,
            offsetX,
            offsetY);

        return new AlignTransformResult(
            matrix,
            sourcePoint1,
            destinationPoint1,
            sourcePoint2,
            destinationPoint2,
            rotationRadians,
            scaleFactor,
            scaleApplied: applyScale,
            isDegenerate: false);
    }
}
