using OpenCad2D.Core.Documents;

namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Imports ASCII DXF content into an OpenCad2D document.
/// </summary>
public interface IDxfImporter
{
    /// <summary>
    /// Imports ASCII DXF content from a string.
    /// </summary>
    DxfImportResult Import(string content);

    /// <summary>
    /// Imports ASCII DXF content from a file.
    /// </summary>
    DxfImportResult ImportFile(string filePath);
}
