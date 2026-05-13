using OpenCad2D.Core.Documents;

namespace OpenCad2D.Export.Pdf;

/// <summary>
/// Exports CAD documents to PDF.
/// </summary>
public interface IPdfExporter
{
    PdfExportResult Export(
        CadDocument document,
        PdfExportOptions? options = null);

    void ExportToFile(
        CadDocument document,
        string filePath,
        PdfExportOptions? options = null);
}
