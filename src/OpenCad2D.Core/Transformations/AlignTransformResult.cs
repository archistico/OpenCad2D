using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Transformations;

/// <summary>
/// Describes the affine transformation computed for an Align operation.
/// </summary>
public sealed class AlignTransformResult
{
    public AlignTransformResult(
        Matrix2D matrix,
        Point2D sourcePoint1,
        Point2D destinationPoint1,
        Point2D sourcePoint2,
        Point2D destinationPoint2,
        Angle rotationAngle,
        double scaleFactor,
        bool scaleApplied,
        bool isDegenerate)
    {
        if (scaleFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scaleFactor),
                "Scale factor must be greater than zero.");
        }

        Matrix = matrix;
        SourcePoint1 = sourcePoint1;
        DestinationPoint1 = destinationPoint1;
        SourcePoint2 = sourcePoint2;
        DestinationPoint2 = destinationPoint2;
        RotationAngle = rotationAngle;
        ScaleFactor = scaleFactor;
        ScaleApplied = scaleApplied;
        IsDegenerate = isDegenerate;
    }

    /// <summary>
    /// Gets the final transformation matrix.
    /// </summary>
    public Matrix2D Matrix { get; }

    /// <summary>
    /// Gets the first source point.
    /// </summary>
    public Point2D SourcePoint1 { get; }

    /// <summary>
    /// Gets the first destination point.
    /// </summary>
    public Point2D DestinationPoint1 { get; }

    /// <summary>
    /// Gets the second source point.
    /// </summary>
    public Point2D SourcePoint2 { get; }

    /// <summary>
    /// Gets the second destination point.
    /// </summary>
    public Point2D DestinationPoint2 { get; }

    /// <summary>
    /// Gets the rotation angle applied by the align operation.
    /// </summary>
    public Angle RotationAngle { get; }

    /// <summary>
    /// Gets the uniform scale factor applied by the align operation.
    /// </summary>
    public double ScaleFactor { get; }

    /// <summary>
    /// Gets a value indicating whether scale was requested and applied.
    /// </summary>
    public bool ScaleApplied { get; }

    /// <summary>
    /// Gets a value indicating whether the operation fell back to translation only
    /// because one of the defining directions has zero length.
    /// </summary>
    public bool IsDegenerate { get; }

    /// <summary>
    /// Transforms a point using the computed matrix.
    /// </summary>
    public Point2D Transform(Point2D point)
    {
        return Matrix.Transform(point);
    }
}
