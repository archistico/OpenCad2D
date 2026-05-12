using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Dimensions.Rendering;

/// <summary>
/// Represents one straight segment used to render a dimension entity.
/// </summary>
public readonly record struct DimensionLinePrimitive(
    Point2D Start,
    Point2D End);
