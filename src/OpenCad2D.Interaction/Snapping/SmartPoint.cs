using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Represents a temporary tracking reference captured from an object snap during an active command.
/// Smart points are viewport/runtime aids only: they are not document entities and are not persisted.
/// </summary>
public sealed class SmartPoint
{
    public SmartPoint(
        Point2D position,
        SnapKind sourceSnapKind,
        EntityId? sourceEntityId,
        DateTimeOffset capturedAt)
    {
        Position = position;
        SourceSnapKind = sourceSnapKind;
        SourceEntityId = sourceEntityId;
        CapturedAt = capturedAt;
    }

    public Point2D Position { get; }

    public SnapKind SourceSnapKind { get; }

    public EntityId? SourceEntityId { get; }

    public DateTimeOffset CapturedAt { get; }
}
