namespace OpenCad2D.Core.Identifiers;

/// <summary>
/// Identifies a reusable single-line text format inside a CAD document.
/// </summary>
public readonly record struct TextFormatId(string Value)
{
    /// <summary>
    /// Identifier of the built-in standard text format.
    /// </summary>
    public static TextFormatId Standard => new("Standard");

    /// <summary>
    /// Identifier of the built-in title text format.
    /// </summary>
    public static TextFormatId Title => new("Title");

    /// <summary>
    /// Identifier of the built-in annotation text format.
    /// </summary>
    public static TextFormatId Annotation => new("Annotation");

    /// <summary>
    /// Identifier of the built-in small text format.
    /// </summary>
    public static TextFormatId Small => new("Small");

    public override string ToString()
    {
        return Value;
    }
}
