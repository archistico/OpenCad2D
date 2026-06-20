using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Linear boundary segment used by the boundary fill graph.
/// Curved source entities are represented by sampled linear segments.
/// </summary>
public readonly record struct BoundarySegment(
    Point2D Start,
    Point2D End,
    EntityId SourceEntityId,
    BoundarySegmentSourceKind SourceKind,
    bool IsSampledCurve = false)
{
    public LineSegment2D ToLineSegment() => new(Start, End);
}
