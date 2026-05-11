using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Allows a tool to override the snap modes that are active for its current phase.
/// This keeps entity-picking snaps separate from geometric point snaps.
/// </summary>
public interface ISnapModeProvider
{
    /// <summary>
    /// Gets the snap modes that should be active now.
    /// </summary>
    SnapKind GetActiveSnapKind(ToolContext context);
}
