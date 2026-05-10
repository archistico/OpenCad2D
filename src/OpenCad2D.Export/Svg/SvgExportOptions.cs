namespace OpenCad2D.Export.Svg;

/// <summary>
/// Options used when exporting a CAD document to SVG.
/// </summary>
public sealed class SvgExportOptions
{
    public double Margin { get; init; } = 20.0;

    public string Title { get; init; } = "OpenCad2D export";

    public bool IncludeHiddenLayers { get; init; } = false;

    public bool IncludeMetadata { get; init; } = true;

    public static SvgExportOptions Default => new();
}
