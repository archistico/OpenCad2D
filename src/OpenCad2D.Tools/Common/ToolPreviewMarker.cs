using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// A model-space point marker drawn as part of a tool preview.
/// </summary>
public readonly record struct ToolPreviewMarker(
    Point2D Position,
    ToolPreviewMarkerKind Kind = ToolPreviewMarkerKind.Primary,
    ToolPreviewMarkerShape Shape = ToolPreviewMarkerShape.Circle);
