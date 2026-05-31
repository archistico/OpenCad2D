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
        double distanceToCursor,
        Point2D? trackingOrigin = null,
        Vector2D? trackingDirection = null)
    {
        if (trackingDirection is not null && trackingDirection.Value.LengthSquared <= 0)
        {
            throw new ArgumentException(
                "The tracking direction cannot be zero.",
                nameof(trackingDirection));
        }

        Kind = kind;
        Point = point;
        EntityId = entityId;
        DistanceToCursor = distanceToCursor;
        TrackingOrigin = trackingOrigin;
        TrackingDirection = trackingDirection?.Normalize();
    }

    public SnapKind Kind { get; }

    public Point2D Point { get; }

    public EntityId? EntityId { get; }

    public double DistanceToCursor { get; }

    /// <summary>
    /// Origin of the temporary tracking or extension line that generated this candidate.
    /// Populated for <see cref="SnapKind.Tracking" /> and <see cref="SnapKind.Extension" /> candidates.
    /// </summary>
    public Point2D? TrackingOrigin { get; }

    /// <summary>
    /// Signed direction from <see cref="TrackingOrigin" /> toward the live cursor side.
    /// Populated for <see cref="SnapKind.Tracking" /> and <see cref="SnapKind.Extension" /> candidates.
    /// </summary>
    public Vector2D? TrackingDirection { get; }
}
