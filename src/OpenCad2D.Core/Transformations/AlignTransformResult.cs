using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Transformations;

/// <summary>
/// Result of an align transformation calculation.
/// </summary>
public sealed class AlignTransformResult
{
    public AlignTransformResult(
        Matrix2D matrix,
        Point2D sourcePoint1,
        Point2D destinationPoint1,
        Point2D sourcePoint2,
        Point2D destinationPoint2,
        double rotationRadians,
        double scaleFactor,
        bool scaleApplied,
        bool isDegenerate)
    {
        Matrix = matrix;
        SourcePoint1 = sourcePoint1;
        DestinationPoint1 = destinationPoint1;
        SourcePoint2 = sourcePoint2;
        DestinationPoint2 = destinationPoint2;
        RotationRadians = rotationRadians;
        ScaleFactor = scaleFactor;
        ScaleApplied = scaleApplied;
        IsDegenerate = isDegenerate;
    }

    public Matrix2D Matrix { get; }

    public Point2D SourcePoint1 { get; }

    public Point2D DestinationPoint1 { get; }

    public Point2D SourcePoint2 { get; }

    public Point2D DestinationPoint2 { get; }

    public double RotationRadians { get; }

    public double RotationDegrees => RotationRadians * 180.0 / Math.PI;

    public double ScaleFactor { get; }

    public bool ScaleApplied { get; }

    public bool IsDegenerate { get; }
}
