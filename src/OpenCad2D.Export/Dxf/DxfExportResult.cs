namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Result of a DXF export operation.
/// </summary>
public sealed class DxfExportResult
{
    public DxfExportResult(
        string content,
        int exportedEntityCount,
        string acadVersion)
    {
        Content = content;
        ExportedEntityCount = exportedEntityCount;
        AcadVersion = acadVersion;
    }

    public string Content { get; }

    public int ExportedEntityCount { get; }

    public string AcadVersion { get; }
}
