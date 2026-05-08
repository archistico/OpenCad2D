namespace OpenCad2D.Interaction.Selection;

/// <summary>
/// Defines how rectangular window selection behaves.
/// </summary>
public enum WindowSelectionMode
{
    /// <summary>
    /// Selects only entities fully contained inside the selection window.
    /// Similar to AutoCAD left-to-right window selection.
    /// </summary>
    Inside,

    /// <summary>
    /// Selects entities whose bounding box intersects the selection window.
    /// Similar to AutoCAD right-to-left crossing selection.
    /// </summary>
    Crossing
}