using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Transformations;

/// <summary>
/// Computes the affine transformation used by the Align tool.
/// </summary>
public sealed class AlignTransformService
{
    /// <summary>
    /// Computes a transformation that maps <paramref name="sourcePoint1" /> to
    /// <paramref name="destinationPoint1" /> and rotates the source direction
    /// toward the destination direction. Optionally, it applies uniform scaling.
    /// </summary>
    public AlignTransformResult CreateTransform(
        Point2D sourcePoint1,
        Point2D destinationPoint1,
        Point2D sourcePoint2,
        Point2D destinationPoint2,
        bool applyScale)
    {
        Vector2D sourceVector = sourcePoint1.VectorTo(sourcePoint2);
        Vector2D destinationVector = destinationPoint1.VectorTo(destinationPoint2);

        double sourceLength = sourceVector.Length;
        double destinationLength = destinationVector.Length;

        if (Tolerance.IsZero(sourceLength) ||
            Tolerance.IsZero(destinationLength))
        {
            Matrix2D translationOnly = Matrix2D.Translation(
                destinationPoint1.X - sourcePoint1.X,
                destinationPoint1.Y - sourcePoint1.Y);

            return new AlignTransformResult(
                translationOnly,
                sourcePoint1,
                destinationPoint1,
                sourcePoint2,
                destinationPoint2,
                Angle.Zero,
                1,
                scaleApplied: false,
                isDegenerate: true);
        }

        double sourceAngle = Math.Atan2(sourceVector.Y, sourceVector.X);
        double destinationAngle = Math.Atan2(destinationVector.Y, destinationVector.X);
        double rotationRadians = destinationAngle - sourceAngle;

        double scaleFactor = applyScale
            ? destinationLength / sourceLength
            : 1;

        Matrix2D matrix = CreateMatrix(
            sourcePoint1,
            destinationPoint1,
            rotationRadians,
            scaleFactor);

        return new AlignTransformResult(
            matrix,
            sourcePoint1,
            destinationPoint1,
            sourcePoint2,
            destinationPoint2,
            Angle.FromRadians(rotationRadians),
            scaleFactor,
            scaleApplied: applyScale,
            isDegenerate: false);
    }

    /// <summary>
    /// Transforms an entity using an Align transform result.
    /// </summary>
    public CadEntity TransformEntity(
        CadEntity entity,
        AlignTransformResult result)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(result);

        return entity.Transform(result.Matrix);
    }

    /// <summary>
    /// Transforms multiple entities using an Align transform result.
    /// </summary>
    public IReadOnlyList<CadEntity> TransformEntities(
        IEnumerable<CadEntity> entities,
        AlignTransformResult result)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(result);

        return entities
            .Select(entity => TransformEntity(entity, result))
            .ToList();
    }

    private static Matrix2D CreateMatrix(
        Point2D sourcePoint,
        Point2D destinationPoint,
        double rotationRadians,
        double scaleFactor)
    {
        if (scaleFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scaleFactor),
                "Scale factor must be greater than zero.");
        }

        double cos = Math.Cos(rotationRadians);
        double sin = Math.Sin(rotationRadians);

        double scaledCos = scaleFactor * cos;
        double scaledSin = scaleFactor * sin;

        double offsetX = destinationPoint.X -
                         scaledCos * sourcePoint.X +
                         scaledSin * sourcePoint.Y;

        double offsetY = destinationPoint.Y -
                         scaledSin * sourcePoint.X -
                         scaledCos * sourcePoint.Y;

        return new Matrix2D(
            scaledCos,
            -scaledSin,
            scaledSin,
            scaledCos,
            offsetX,
            offsetY);
    }
}
