namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Identifies the behavior associated with a grip point.
/// </summary>
public enum GripKind
{
    /// <summary>
    /// Moves a single geometric point, such as a line endpoint.
    /// </summary>
    MoveVertex,

    /// <summary>
    /// Moves the entire entity rigidly.
    /// </summary>
    MoveEntity,

    /// <summary>
    /// Resizes a radial entity, such as a circle radius.
    /// </summary>
    ResizeRadius
}
