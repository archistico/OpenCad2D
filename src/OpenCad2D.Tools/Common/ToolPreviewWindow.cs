using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// A model-space rectangular window drawn as part of a tool preview.
/// </summary>
public readonly record struct ToolPreviewWindow(
    BoundingBox2D Bounds,
    ToolPreviewWindowKind Kind = ToolPreviewWindowKind.Selection);
