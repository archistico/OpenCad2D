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

    /// <summary>
    /// Gets whether a background rectangle should be emitted as the first SVG shape.
    /// </summary>
    public bool IncludeBackground { get; init; } = true;

    /// <summary>
    /// Gets the exported background color. The default matches the OpenCad2D canvas background.
    /// </summary>
    public string BackgroundColor { get; init; } = "#1E1E1E";

    public static SvgExportOptions Default => new();
}
