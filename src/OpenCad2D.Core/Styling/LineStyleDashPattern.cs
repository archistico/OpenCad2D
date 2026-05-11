namespace OpenCad2D.Core.Styling;

/// <summary>
/// Provides model-space dash patterns for line styles.
/// </summary>
public static class LineStyleDashPattern
{
    /// <summary>
    /// Gets the dash pattern expressed in model units.
    /// A null value means that the line is continuous.
    /// </summary>
    public static double[]? Get(LineStyle style)
    {
        return style switch
        {
            LineStyle.Continuous => null,
            LineStyle.Dashed => [6.0, 3.0],
            LineStyle.DashDot => [6.0, 2.0, 1.0, 2.0],
            LineStyle.DashDotDot => [6.0, 2.0, 1.0, 2.0, 1.0, 2.0],
            _ => null,
        };
    }
}
