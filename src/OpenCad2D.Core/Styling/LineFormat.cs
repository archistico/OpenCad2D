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
        LineStyle lineStyle)
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

        Id = id;
        Name = name.Trim();
        Color = color;
        LineWeight = lineWeight;
        LineStyle = lineStyle;
    }

    public LineFormatId Id { get; }

    public string Name { get; }

    public CadColor Color { get; }

    public LineWeight LineWeight { get; }

    public LineStyle LineStyle { get; }

    /// <summary>
    /// Gets whether the format is a built-in format that cannot be deleted.
    /// Built-in formats may still be renamed or edited.
    /// </summary>
    public bool IsBuiltIn =>
        Id == LineFormatId.Continuous ||
        Id == LineFormatId.Dashed ||
        Id == LineFormatId.DashDot ||
        Id == LineFormatId.DashDotDot ||
        Id == LineFormatId.Axis;

    public LineFormat WithName(string name)
    {
        return new LineFormat(
            Id,
            name,
            Color,
            LineWeight,
            LineStyle);
    }

    public LineFormat WithAppearance(
        CadColor color,
        LineWeight lineWeight,
        LineStyle lineStyle)
    {
        return new LineFormat(
            Id,
            Name,
            color,
            lineWeight,
            lineStyle);
    }
}
