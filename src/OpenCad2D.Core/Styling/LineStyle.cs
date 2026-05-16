namespace OpenCad2D.Core.Styling;

/// <summary>
/// Defines the semantic line style associated with a reusable line format.
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

    /// <summary>
    /// User-defined dash pattern stored on the line format.
    /// </summary>
    Custom,
}
