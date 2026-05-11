using OpenCad2D.Core.Documents;

namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Exports CAD documents to ASCII DXF.
/// </summary>
public interface IDxfExporter
{
    DxfExportResult Export(
        CadDocument document,
        DxfExportOptions? options = null);

    void ExportToFile(
        CadDocument document,
        string filePath,
        DxfExportOptions? options = null);
}
