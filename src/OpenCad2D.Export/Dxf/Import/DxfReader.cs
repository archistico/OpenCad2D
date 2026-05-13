using System.Text;

namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Reads ASCII DXF content as group-code pairs.
/// </summary>
public sealed class DxfReader
{
    /// <summary>
    /// Reads ASCII DXF group-code pairs from a string.
    /// </summary>
    public IReadOnlyList<DxfCodePair> Read(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var stringReader = new StringReader(content);

        return Read(stringReader);
    }

    /// <summary>
    /// Reads ASCII DXF group-code pairs from a file using ASCII encoding.
    /// </summary>
    public IReadOnlyList<DxfCodePair> ReadFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "DXF file path cannot be empty.",
                nameof(filePath));
        }

        using var reader = new StreamReader(
            filePath,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: true);

        return Read(reader);
    }

    /// <summary>
    /// Reads ASCII DXF group-code pairs from a text reader.
    /// </summary>
    public IReadOnlyList<DxfCodePair> Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var pairs = new List<DxfCodePair>();
        int lineNumber = 0;

        while (true)
        {
            string? codeLine = reader.ReadLine();

            if (codeLine is null)
            {
                break;
            }

            lineNumber++;
            int codeLineNumber = lineNumber;

            if (!int.TryParse(codeLine.Trim(), out int code))
            {
                throw new DxfReadException(
                    $"Invalid DXF group code at line {codeLineNumber}: '{codeLine}'.");
            }

            string? valueLine = reader.ReadLine();

            if (valueLine is null)
            {
                throw new DxfReadException(
                    $"Missing DXF value after group code at line {codeLineNumber}.");
            }

            lineNumber++;
            pairs.Add(new DxfCodePair(
                code,
                valueLine.Trim(),
                codeLineNumber,
                lineNumber));
        }

        return pairs;
    }
}
