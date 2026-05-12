using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Dimensions.Rendering;

/// <summary>
/// Represents the formatted text used to render a dimension measurement.
/// </summary>
public readonly record struct DimensionTextPrimitive(
    string Text,
    Point2D Position,
    double RotationDegrees);
