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
        Statistics = DxfImportStatistics.From(Document, Log);
    }

    public CadDocument Document { get; }

    public IReadOnlyList<DxfImportLogEntry> Log { get; }

    public DxfImportStatistics Statistics { get; }

    public bool HasWarnings => Statistics.WarningCount > 0;

    public bool HasErrors => Statistics.ErrorCount > 0;
}
