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

    public override string ToString()
    {
        return Value;
    }
}
