namespace OpenCad2D.Core.Identifiers;

/// <summary>
/// Identifies a reusable line format inside a CAD document.
/// </summary>
public readonly record struct LineFormatId(string Value)
{
    /// <summary>
    /// Sentinel used when a visual property is resolved from the owning layer.
    /// </summary>
    public static LineFormatId ByLayer => new("ByLayer");

    /// <summary>
    /// Identifier of the built-in continuous line format.
    /// </summary>
    public static LineFormatId Continuous => new("Continuous");

    /// <summary>
    /// Identifier of the built-in dashed line format.
    /// </summary>
    public static LineFormatId Dashed => new("Dashed");

    /// <summary>
    /// Identifier of the built-in dash-dot line format.
    /// </summary>
    public static LineFormatId DashDot => new("DashDot");

    /// <summary>
    /// Identifier of the built-in dash-dot-dot line format.
    /// </summary>
    public static LineFormatId DashDotDot => new("DashDotDot");

    /// <summary>
    /// Identifier of the built-in axis line format.
    /// </summary>
    public static LineFormatId Axis => new("Axis");

    public static LineFormatId Annotations => new("Annotations");

    public static LineFormatId Walls => new("Walls");

    public override string ToString()
    {
        return Value;
    }
}
