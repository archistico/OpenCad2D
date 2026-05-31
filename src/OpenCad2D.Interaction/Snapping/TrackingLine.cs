using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Represents a temporary infinite tracking line generated from a smart point.
/// Tracking lines are viewport/runtime aids only and are never persisted.
/// </summary>
public sealed class TrackingLine
{
    public TrackingLine(
        Point2D origin,
        Vector2D direction,
        TrackingLineKind kind,
        SmartPoint sourcePoint)
    {
        if (direction.LengthSquared <= 0)
        {
            throw new ArgumentException("The tracking line direction cannot be zero.", nameof(direction));
        }

        Origin = origin;
        Direction = direction.Normalize();
        Kind = kind;
        SourcePoint = sourcePoint ?? throw new ArgumentNullException(nameof(sourcePoint));
    }

    public Point2D Origin { get; }

    public Vector2D Direction { get; }

    public TrackingLineKind Kind { get; }

    public SmartPoint SourcePoint { get; }
}
