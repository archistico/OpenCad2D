using OpenCad2D.Core.Documents;

namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Result of a DXF import operation.
/// </summary>
public sealed class DxfImportResult
{
    public DxfImportResult(
        CadDocument document,
        IReadOnlyList<DxfImportLogEntry> log)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public CadDocument Document { get; }

    public IReadOnlyList<DxfImportLogEntry> Log { get; }

    public bool HasWarnings => Log.Any(entry => entry.Severity == DxfImportLogSeverity.Warning);

    public bool HasErrors => Log.Any(entry => entry.Severity == DxfImportLogSeverity.Error);
}
