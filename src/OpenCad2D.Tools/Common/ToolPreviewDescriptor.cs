using OpenCad2D.Core.Entities;

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
        IEnumerable<ToolPreviewLine>? lines = null,
        IEnumerable<ToolPreviewMarker>? markers = null,
        IEnumerable<ToolPreviewWindow>? windows = null)
    {
        Entities = entities?.ToArray() ?? Array.Empty<CadEntity>();
        HighlightedEntities = highlightedEntities?.ToArray() ?? Array.Empty<CadEntity>();
        Lines = lines?.ToArray() ?? Array.Empty<ToolPreviewLine>();
        Markers = markers?.ToArray() ?? Array.Empty<ToolPreviewMarker>();
        Windows = windows?.ToArray() ?? Array.Empty<ToolPreviewWindow>();
    }

    public IReadOnlyList<CadEntity> Entities { get; }

    public IReadOnlyList<CadEntity> HighlightedEntities { get; }

    public IReadOnlyList<ToolPreviewLine> Lines { get; }

    public IReadOnlyList<ToolPreviewMarker> Markers { get; }

    public IReadOnlyList<ToolPreviewWindow> Windows { get; }

    public bool IsEmpty =>
        Entities.Count == 0 &&
        HighlightedEntities.Count == 0 &&
        Lines.Count == 0 &&
        Markers.Count == 0 &&
        Windows.Count == 0;

    public static ToolPreviewDescriptor FromEntities(
        IEnumerable<CadEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return new ToolPreviewDescriptor(entities: entities);
    }
}
