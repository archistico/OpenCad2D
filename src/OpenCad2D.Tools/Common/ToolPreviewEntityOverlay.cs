using OpenCad2D.Core.Entities;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Describes an additional semantic entity overlay for a tool preview.
/// </summary>
public sealed class ToolPreviewEntityOverlay
{
    public ToolPreviewEntityOverlay(
        IEnumerable<CadEntity> entities,
        ToolPreviewHighlightKind kind = ToolPreviewHighlightKind.Emphasis)
    {
        ArgumentNullException.ThrowIfNull(entities);

        Entities = entities.ToArray();
        Kind = kind;
    }

    public IReadOnlyList<CadEntity> Entities { get; }

    public ToolPreviewHighlightKind Kind { get; }
}
