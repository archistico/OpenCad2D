using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Base class for non-associative dimension entities.
/// </summary>
public abstract class DimensionEntity : CadEntity
{
    protected DimensionEntity(
        DimensionStyleId? dimensionStyleId,
        string? textOverride,
        EntityId id,
        LayerId layerId,
        EntityStyle style,
        bool isVisible,
        bool isLocked,
        int drawOrder)
        : base(
            id,
            layerId,
            style,
            isVisible,
            isLocked,
            drawOrder)
    {
        DimensionStyleId resolvedStyleId = dimensionStyleId ?? DimensionStyleId.Standard;

        if (string.IsNullOrWhiteSpace(resolvedStyleId.Value))
        {
            throw new ArgumentException(
                "Dimension style id cannot be empty.",
                nameof(dimensionStyleId));
        }

        DimensionStyleId = resolvedStyleId;
        TextOverride = string.IsNullOrWhiteSpace(textOverride)
            ? null
            : textOverride.Trim();
    }

    public DimensionStyleId DimensionStyleId { get; }

    public string? TextOverride { get; }

    public abstract double MeasurementValue { get; }

    public string GetDisplayText(int decimalPlaces = 2, string decimalSeparator = ".", string suffix = "")
    {
        if (!string.IsNullOrWhiteSpace(TextOverride))
        {
            return TextOverride;
        }

        string format = "F" + Math.Clamp(decimalPlaces, 0, 8);
        string text = MeasurementValue.ToString(
            format,
            System.Globalization.CultureInfo.InvariantCulture);

        if (decimalSeparator == ",")
        {
            text = text.Replace('.', ',');
        }

        return text + suffix;
    }
}
