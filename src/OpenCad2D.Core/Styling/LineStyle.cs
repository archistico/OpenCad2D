namespace OpenCad2D.Core.Styling;

/// <summary>
/// Defines how a stroke is rendered along its path.
/// </summary>
public enum LineStyle
{
    /// <summary>
    /// Unbroken solid stroke.
    /// </summary>
    Continuous,

    /// <summary>
    /// Equal-length dashes separated by gaps.
    /// </summary>
    Dashed,

    /// <summary>
    /// Long dash, short gap, dot, short gap, repeat.
    /// </summary>
    DashDot,

    /// <summary>
    /// Long dash, short gap, dot, short gap, dot, short gap, repeat.
    /// </summary>
    DashDotDot,
}
