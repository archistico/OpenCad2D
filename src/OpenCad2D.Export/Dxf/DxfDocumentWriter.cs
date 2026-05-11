using System.Globalization;
using System.Text;

namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Writes ASCII DXF group-code pairs.
/// </summary>
public sealed class DxfDocumentWriter
{
    private readonly StringBuilder _builder = new();

    /// <summary>
    /// Writes a DXF group-code pair with a string value.
    /// </summary>
    public void WriteGroup(
        int code,
        string value)
    {
        _builder.AppendLine(code.ToString(CultureInfo.InvariantCulture));
        _builder.AppendLine(value ?? string.Empty);
    }

    /// <summary>
    /// Writes a DXF group-code pair with an integer value.
    /// </summary>
    public void WriteGroup(
        int code,
        int value)
    {
        WriteGroup(
            code,
            value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Writes a DXF group-code pair with a floating-point value.
    /// </summary>
    public void WriteGroup(
        int code,
        double value)
    {
        WriteGroup(
            code,
            Format(value));
    }

    /// <summary>
    /// Starts a DXF section.
    /// </summary>
    public void BeginSection(string name)
    {
        WriteGroup(0, "SECTION");
        WriteGroup(2, name);
    }

    /// <summary>
    /// Ends the current DXF section.
    /// </summary>
    public void EndSection()
    {
        WriteGroup(0, "ENDSEC");
    }

    /// <summary>
    /// Writes the DXF end-of-file marker.
    /// </summary>
    public void WriteEndOfFile()
    {
        WriteGroup(0, "EOF");
    }

    /// <summary>
    /// Returns the generated ASCII DXF content.
    /// </summary>
    public override string ToString()
    {
        return _builder.ToString();
    }

    private static string Format(double value)
    {
        return value.ToString(
            "0.###############",
            CultureInfo.InvariantCulture);
    }
}
