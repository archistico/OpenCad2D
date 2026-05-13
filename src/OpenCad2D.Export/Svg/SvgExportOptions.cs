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
    /// This property is kept for compatibility with earlier export code.
    /// When set to <c>false</c>, it has the same effect as <see cref="SvgBackgroundMode.Transparent" />.
    /// </summary>
    public bool IncludeBackground { get; init; } = true;

    /// <summary>
    /// Gets the exported background mode.
    /// </summary>
    public SvgBackgroundMode BackgroundMode { get; init; } = SvgBackgroundMode.CanvasDark;

    /// <summary>
    /// Gets the exported canvas background color when <see cref="BackgroundMode" /> is
    /// <see cref="SvgBackgroundMode.CanvasDark" />. The default matches the OpenCad2D canvas background.
    /// </summary>
    public string BackgroundColor { get; init; } = "#1E1E1E";

    /// <summary>
    /// Gets whether exported entities should be grouped by layer using SVG <c>g</c> elements.
    /// </summary>
    public bool GroupByLayer { get; init; } = false;

    public static SvgExportOptions Default => new();
}
