namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// One diagnostic message produced by the DXF import pipeline.
/// </summary>
public sealed class DxfImportLogEntry
{
    public DxfImportLogEntry(
        DxfImportLogSeverity severity,
        string message,
        int? lineNumber = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "DXF import log message cannot be empty.",
                nameof(message));
        }

        Severity = severity;
        Message = message.Trim();
        LineNumber = lineNumber;
    }

    public DxfImportLogSeverity Severity { get; }

    public string Message { get; }

    public int? LineNumber { get; }
}
