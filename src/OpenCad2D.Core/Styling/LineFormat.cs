using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Styling;

/// <summary>
/// Defines a named, reusable stroke format assignable to one or more layers.
/// </summary>
public sealed class LineFormat
{
    public LineFormat(
        LineFormatId id,
        string name,
        CadColor color,
        LineWeight lineWeight,
        LineStyle lineStyle,
        IEnumerable<double>? dashPattern = null)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException(
                "LineFormat id cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "LineFormat name cannot be empty.",
                nameof(name));
        }

        List<double> normalizedDashPattern = NormalizeDashPattern(
            lineStyle,
            dashPattern);

        Id = id;
        Name = name.Trim();
        Color = color;
        LineWeight = lineWeight;
        LineStyle = lineStyle;
        DashPattern = normalizedDashPattern;
    }

    public LineFormatId Id { get; }

    public string Name { get; }

    public CadColor Color { get; }

    public LineWeight LineWeight { get; }

    public LineStyle LineStyle { get; }

    /// <summary>
    /// Gets the dash pattern expressed in model/drawing units.
    /// Empty means a continuous stroke.
    /// </summary>
    public IReadOnlyList<double> DashPattern { get; }

    /// <summary>
    /// Gets whether the format is a built-in format that cannot be deleted.
    /// Built-in formats may still be renamed or edited.
    /// </summary>
    public bool IsBuiltIn =>
        Id == LineFormatId.Continuous ||
        Id == LineFormatId.Dashed ||
        Id == LineFormatId.DashDot ||
        Id == LineFormatId.DashDotDot ||
        Id == LineFormatId.Axis ||
        Id == LineFormatId.Annotations ||
        Id == LineFormatId.Walls;

    public LineFormat WithName(string name)
    {
        return new LineFormat(
            Id,
            name,
            Color,
            LineWeight,
            LineStyle,
            DashPattern);
    }

    public LineFormat WithAppearance(
        CadColor color,
        LineWeight lineWeight,
        LineStyle lineStyle,
        IEnumerable<double>? dashPattern = null)
    {
        return new LineFormat(
            Id,
            Name,
            color,
            lineWeight,
            lineStyle,
            dashPattern ?? LineStyleDashPattern.Get(lineStyle));
    }

    private static List<double> NormalizeDashPattern(
        LineStyle lineStyle,
        IEnumerable<double>? dashPattern)
    {
        IReadOnlyList<double>? pattern = dashPattern?.ToList();

        if (pattern is null)
        {
            pattern = LineStyleDashPattern.Get(lineStyle);
        }

        if (!LineStyleDashPattern.IsValid(pattern))
        {
            throw new ArgumentException(
                "Dash pattern values must be positive dash/gap pairs expressed in drawing units.",
                nameof(dashPattern));
        }

        return pattern is null
            ? []
            : pattern.ToList();
    }
}
