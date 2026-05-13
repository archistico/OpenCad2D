namespace OpenCad2D.Export.Svg;

/// <summary>
/// Defines how the SVG exporter writes the drawing background.
/// </summary>
public enum SvgBackgroundMode
{
    /// <summary>
    /// Uses the OpenCad2D canvas-style dark background.
    /// </summary>
    CanvasDark,

    /// <summary>
    /// Uses a white background, useful for print-friendly or document exports.
    /// </summary>
    White,

    /// <summary>
    /// Does not emit a background rectangle.
    /// </summary>
    Transparent
}
