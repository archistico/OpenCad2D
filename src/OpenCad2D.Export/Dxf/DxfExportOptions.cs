namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Options used when exporting a CAD document to DXF.
/// </summary>
public sealed class DxfExportOptions
{
    /// <summary>
    /// Gets the DXF version written in the HEADER section.
    /// AC1015 identifies AutoCAD 2000 ASCII DXF.
    /// </summary>
    public string AcadVersion { get; init; } = "AC1015";

    /// <summary>
    /// Gets whether entities on hidden layers should be exported.
    /// Entity export is implemented in later phases, but the option is kept here
    /// so the DXF API matches the SVG exporter shape from the start.
    /// </summary>
    public bool IncludeHiddenLayers { get; init; } = false;

    public static DxfExportOptions Default => new();
}
