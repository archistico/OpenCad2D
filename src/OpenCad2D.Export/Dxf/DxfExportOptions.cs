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

    /// <summary>
    /// Gets whether Y coordinates and angular values should be transformed for
    /// conventional CAD viewers that use an upward-positive Y axis.
    /// Keep this enabled for normal interoperability exports. Disable it only for
    /// model-coordinate round-trip tests or internal diagnostics.
    /// </summary>
    public bool UseCadViewerCoordinateSystem { get; init; } = true;

    public static DxfExportOptions Default => new();
}
