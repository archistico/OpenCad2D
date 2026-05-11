namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Describes the geometric layout used for grid rendering and grid snapping.
/// </summary>
public enum GridKind
{
    /// <summary>
    /// Standard orthogonal grid with horizontal and vertical lines.
    /// </summary>
    Rectangular = 0,

    /// <summary>
    /// Isometric grid with vertical lines and two diagonal families.
    /// </summary>
    Isometric = 1
}
