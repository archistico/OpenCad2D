using OpenCad2D.Core.Entities;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides transient preview entities for tools that can describe their own preview geometry.
/// </summary>
/// <remarks>
/// This keeps preview geometry creation close to the tool state and lets UI renderers avoid
/// depending on concrete tool types for simple entity-based previews.
/// </remarks>
public interface IToolPreviewEntityProvider
{
    /// <summary>
    /// Gets the current transient preview entities for the tool.
    /// </summary>
    IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context);
}
