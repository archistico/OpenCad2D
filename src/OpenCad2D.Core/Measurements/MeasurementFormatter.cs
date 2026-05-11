using System.Globalization;

namespace OpenCad2D.Core.Measurements;

/// <summary>
/// Formats measurement values for command/status output.
/// OpenCad2D model space has no physical unit, so no unit suffix is appended.
/// </summary>
public static class MeasurementFormatter
{
    private const string NumericFormat = "0.###";

    public static string FormatDistance(DistanceMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Distance: {FormatNumber(measurement.Distance)} | ΔX: {FormatNumber(measurement.DeltaX)} | ΔY: {FormatNumber(measurement.DeltaY)} | Angle: {FormatNumber(measurement.AngleDegrees)}°");
    }

    public static string FormatAngle(AngleMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Angle: {FormatNumber(measurement.Degrees)}° | Supplementary: {FormatNumber(measurement.SupplementaryDegrees)}°");
    }

    public static string FormatEntity(EntityMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        var parts = new List<string>
        {
            measurement.EntityKind.ToString()
        };

        Add(parts, "Length", measurement.Length);
        Add(parts, "Angle", measurement.AngleDegrees, "°");
        Add(parts, "Radius", measurement.Radius);
        Add(parts, "Diameter", measurement.Diameter);
        Add(parts, "Circumference", measurement.Circumference);
        Add(parts, "Area", measurement.Area);
        Add(parts, "Sweep", measurement.SweepAngleDegrees, "°");

        if (measurement.VertexCount is not null)
        {
            parts.Add($"Vertices: {measurement.VertexCount.Value}");
        }

        if (measurement.IsClosed is not null)
        {
            parts.Add($"Closed: {(measurement.IsClosed.Value ? "Yes" : "No")}");
        }

        return string.Join(" | ", parts);
    }

    private static void Add(
        ICollection<string> parts,
        string label,
        double? value,
        string suffix = "")
    {
        if (value is null)
        {
            return;
        }

        parts.Add($"{label}: {FormatNumber(value.Value)}{suffix}");
    }

    private static string FormatNumber(double value)
    {
        return value.ToString(NumericFormat, CultureInfo.InvariantCulture);
    }
}
