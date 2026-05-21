namespace OpenCad2D.Core.Identifiers;

/// <summary>
/// Identifies a reusable dimension style inside a CAD document.
/// </summary>
public readonly record struct DimensionStyleId(string Value)
{
    /// <summary>
    /// Identifier of the built-in standard dimension style.
    /// </summary>
    public static DimensionStyleId Standard => new("Standard");

    /// <summary>
    /// Identifier of the built-in architectural dimension style.
    /// </summary>
    public static DimensionStyleId Architectural => new("Architectural");

    /// <summary>
    /// Identifier of the built-in mechanical dimension style.
    /// </summary>
    public static DimensionStyleId Mechanical => new("Mechanical");

    public override string ToString()
    {
        return Value;
    }
}
