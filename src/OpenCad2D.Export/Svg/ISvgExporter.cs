using OpenCad2D.Core.Documents;

namespace OpenCad2D.Export.Svg;

/// <summary>
/// Exports CAD documents to SVG.
/// </summary>
public interface ISvgExporter
{
    SvgExportResult Export(
        CadDocument document,
        SvgExportOptions? options = null);

    void ExportToFile(
        CadDocument document,
        string filePath,
        SvgExportOptions? options = null);
}
