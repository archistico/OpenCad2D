using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Describes one editable grip point for one CAD entity.
/// </summary>
public readonly record struct GripPoint(
    Point2D Position,
    GripKind Kind,
    EntityId EntityId,
    int GripIndex);
