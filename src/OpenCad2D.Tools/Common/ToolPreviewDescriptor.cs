using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Describes the complete transient preview emitted by an interactive tool.
/// </summary>
/// <remarks>
/// The descriptor is intentionally UI-agnostic: tools describe model-space
/// preview geometry and semantic overlay items, while the application decides
/// how to render them.
/// </remarks>
public sealed class ToolPreviewDescriptor
{
    public static ToolPreviewDescriptor Empty { get; } = new();

    public ToolPreviewDescriptor(
        IEnumerable<CadEntity>? entities = null,
        IEnumerable<CadEntity>? highlightedEntities = null,
        ToolPreviewHighlightKind highlightedEntityKind = ToolPreviewHighlightKind.Emphasis,
        IEnumerable<ToolPreviewLine>? lines = null,
        IEnumerable<ToolPreviewMarker>? markers = null,
        IEnumerable<ToolPreviewWindow>? windows = null,
        IEnumerable<ToolPreviewEntityOverlay>? entityOverlays = null,
        ToolPreviewHighlightKind? entityHighlightKind = null,
        IEnumerable<EntityId>? replacedEntityIds = null)
    {
        Entities = entities?.ToArray() ?? Array.Empty<CadEntity>();
        HighlightedEntities = highlightedEntities?.ToArray() ?? Array.Empty<CadEntity>();
        HighlightedEntityKind = highlightedEntityKind;
        Lines = lines?.ToArray() ?? Array.Empty<ToolPreviewLine>();
        Markers = markers?.ToArray() ?? Array.Empty<ToolPreviewMarker>();
        Windows = windows?.ToArray() ?? Array.Empty<ToolPreviewWindow>();
        EntityOverlays = entityOverlays?.ToArray() ?? Array.Empty<ToolPreviewEntityOverlay>();
        EntityHighlightKind = entityHighlightKind;
        ReplacedEntityIds = replacedEntityIds?.ToArray() ?? Array.Empty<EntityId>();
    }

    public IReadOnlyList<CadEntity> Entities { get; }

    public ToolPreviewHighlightKind? EntityHighlightKind { get; }

    /// <summary>
    /// Document entities visually replaced by this transient preview, without changing the document.
    /// </summary>
    public IReadOnlyList<EntityId> ReplacedEntityIds { get; }

    public IReadOnlyList<CadEntity> HighlightedEntities { get; }

    public ToolPreviewHighlightKind HighlightedEntityKind { get; }

    public IReadOnlyList<ToolPreviewLine> Lines { get; }

    public IReadOnlyList<ToolPreviewMarker> Markers { get; }

    public IReadOnlyList<ToolPreviewWindow> Windows { get; }

    public IReadOnlyList<ToolPreviewEntityOverlay> EntityOverlays { get; }

    public bool IsEmpty =>
        Entities.Count == 0 &&
        HighlightedEntities.Count == 0 &&
        Lines.Count == 0 &&
        Markers.Count == 0 &&
        Windows.Count == 0 &&
        EntityOverlays.Count == 0 &&
        ReplacedEntityIds.Count == 0;

    public static ToolPreviewDescriptor FromEntities(
        IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return new ToolPreviewDescriptor(entities: entities);
    }
}
