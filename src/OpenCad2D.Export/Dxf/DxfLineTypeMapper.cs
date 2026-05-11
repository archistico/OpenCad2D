using OpenCad2D.Core.Styling;

namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Maps OpenCad2D line styles to DXF linetype names and patterns.
/// </summary>
public static class DxfLineTypeMapper
{
    public static string ToDxfName(LineStyle style)
    {
        return style switch
        {
            LineStyle.Continuous => "CONTINUOUS",
            LineStyle.Dashed => "DASHED",
            LineStyle.DashDot => "DASHDOT",
            LineStyle.DashDotDot => "DASHDOTDOT",
            _ => "CONTINUOUS",
        };
    }

    public static IReadOnlyList<double> GetPattern(LineStyle style)
    {
        return style switch
        {
            LineStyle.Continuous => [],
            LineStyle.Dashed => [6.0, -3.0],
            LineStyle.DashDot => [6.0, -2.0, 0.0, -2.0],
            LineStyle.DashDotDot => [6.0, -2.0, 0.0, -2.0, 0.0, -2.0],
            _ => [],
        };
    }

    public static IReadOnlyList<LineStyle> AllExportedStyles =>
    [
        LineStyle.Continuous,
        LineStyle.Dashed,
        LineStyle.DashDot,
        LineStyle.DashDotDot,
    ];
}
