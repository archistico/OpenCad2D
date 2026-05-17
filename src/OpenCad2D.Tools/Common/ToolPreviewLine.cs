using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// A model-space guide line drawn as part of a tool preview.
/// </summary>
public readonly record struct ToolPreviewLine(
    Point2D Start,
    Point2D End,
    ToolPreviewLineKind Kind = ToolPreviewLineKind.Guide);
