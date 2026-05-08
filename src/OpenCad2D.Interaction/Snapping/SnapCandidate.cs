using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Represents a possible snap point found near the cursor.
/// </summary>
public sealed class SnapCandidate
{
    public SnapCandidate(
        SnapKind kind,
        Point2D point,
        EntityId? entityId,
        double distanceToCursor)
    {
        Kind = kind;
        Point = point;
        EntityId = entityId;
        DistanceToCursor = distanceToCursor;
    }

    public SnapKind Kind { get; }

    public Point2D Point { get; }

    public EntityId? EntityId { get; }

    public double DistanceToCursor { get; }
}