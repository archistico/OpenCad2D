namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Represents one ASCII DXF group-code pair.
/// </summary>
public readonly record struct DxfCodePair(
    int Code,
    string Value,
    int CodeLineNumber,
    int ValueLineNumber)
{
    /// <summary>
    /// Gets whether this pair represents a DXF structural marker with group code 0.
    /// </summary>
    public bool IsMarker => Code == 0;

    /// <summary>
    /// Returns true when this pair is a group-code 0 marker with the requested value.
    /// </summary>
    public bool IsMarkerValue(string value)
    {
        return IsMarker &&
               string.Equals(
                   Value,
                   value,
                   StringComparison.OrdinalIgnoreCase);
    }
}
