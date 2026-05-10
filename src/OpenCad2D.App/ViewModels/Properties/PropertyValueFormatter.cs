using OpenCad2D.Geometry.Primitives;
using System.Globalization;

namespace OpenCad2D.App.ViewModels.Properties;

public static class PropertyValueFormatter
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string FormatCoordinate(double value)
    {
        return value.ToString("0.###", Culture);
    }

    public static string FormatLength(double value)
    {
        return value.ToString("0.###", Culture);
    }

    public static string FormatArea(double value)
    {
        return value.ToString("0.###", Culture);
    }

    public static string FormatAngleDegrees(double value)
    {
        return value.ToString("0.##", Culture) + "°";
    }

    public static string FormatPoint(Point2D point)
    {
        return $"X {FormatCoordinate(point.X)}, Y {FormatCoordinate(point.Y)}";
    }

    public static string FormatBoolean(bool value)
    {
        return value ? "Yes" : "No";
    }
}
