namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Represents a logical DXF section such as HEADER, TABLES or ENTITIES.
/// </summary>
public sealed class DxfSection
{
    public DxfSection(
        string name,
        IReadOnlyList<DxfCodePair> pairs,
        int startLineNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "DXF section name cannot be empty.",
                nameof(name));
        }

        Name = name.Trim().ToUpperInvariant();
        Pairs = pairs ?? throw new ArgumentNullException(nameof(pairs));
        StartLineNumber = startLineNumber;
    }

    /// <summary>
    /// Gets the normalized DXF section name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets all group-code pairs contained in this section, excluding SECTION/ENDSEC markers.
    /// </summary>
    public IReadOnlyList<DxfCodePair> Pairs { get; }

    /// <summary>
    /// Gets the source line where the SECTION marker starts.
    /// </summary>
    public int StartLineNumber { get; }
}
