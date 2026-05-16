namespace OpenCad2D.Core.Styling;

/// <summary>
/// Provides model-space dash patterns for line styles.
/// </summary>
public static class LineStyleDashPattern
{
    /// <summary>
    /// Gets the default dash pattern expressed in model units.
    /// A null value means that the line is continuous.
    /// </summary>
    public static double[]? Get(LineStyle style)
    {
        return style switch
        {
            LineStyle.Continuous => null,
            LineStyle.Dashed => [8.0, 4.0],
            LineStyle.DashDot => [12.0, 4.0, 1.0, 4.0],
            LineStyle.DashDotDot => [12.0, 4.0, 1.0, 4.0, 1.0, 4.0],
            LineStyle.Custom => null,
            _ => null,
        };
    }

    /// <summary>
    /// Gets whether a custom dash pattern is usable for CAD rendering/export.
    /// Patterns are expressed in drawing units and must be dash/gap pairs.
    /// </summary>
    public static bool IsValid(IReadOnlyList<double>? pattern)
    {
        if (pattern is null || pattern.Count == 0)
        {
            return true;
        }

        if (pattern.Count % 2 != 0)
        {
            return false;
        }

        return pattern.All(value =>
            !double.IsNaN(value) &&
            !double.IsInfinity(value) &&
            value > 0);
    }
}
