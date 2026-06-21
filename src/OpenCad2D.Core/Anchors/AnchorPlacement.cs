using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Anchors;

/// <summary>
/// Result of placing a local bounding box so that a selected anchor coincides
/// with a target insertion point in world coordinates.
/// </summary>
public sealed record AnchorPlacement(
    AnchorPoint Anchor,
    Point2D LocalAnchorPoint,
    Point2D TargetPoint,
    Vector2D Translation);
