namespace OpenCad2D.Tools.Common;

/// <summary>
/// Describes the semantic purpose of highlighted preview entities.
/// </summary>
public enum ToolPreviewHighlightKind
{
    /// <summary>
    /// Generic highlighted transient geometry.
    /// </summary>
    Emphasis,

    /// <summary>
    /// Geometry that will be added if the current command is confirmed.
    /// </summary>
    Addition,

    /// <summary>
    /// Geometry that will be removed if the current command is confirmed.
    /// </summary>
    Removal,

    /// <summary>
    /// Native fragments that will remain after trimming the hovered target.
    /// </summary>
    TrimRemainder,

    /// <summary>
    /// Native interval that will be removed from the hovered trim target.
    /// </summary>
    TrimRemoval
}
