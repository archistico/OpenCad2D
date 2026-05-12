using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Dimensions;

/// <summary>
/// Defines the reusable visual and numeric settings used by dimension entities.
/// </summary>
public sealed class DimensionStyle
{
    public DimensionStyle(
        DimensionStyleId id,
        string name,
        TextFormatId textFormatId,
        double arrowSize,
        double textOffset,
        double extensionLineOffset,
        double extensionLineOvershoot,
        int decimalPlaces,
        string decimalSeparator = ".",
        string suffix = "")
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException(
                "Dimension style id cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Dimension style name cannot be empty.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(textFormatId.Value))
        {
            throw new ArgumentException(
                "Dimension style text format id cannot be empty.",
                nameof(textFormatId));
        }

        if (arrowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(arrowSize),
                arrowSize,
                "Dimension style arrow size must be greater than zero.");
        }

        if (textOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(textOffset),
                textOffset,
                "Dimension style text offset cannot be negative.");
        }

        if (extensionLineOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(extensionLineOffset),
                extensionLineOffset,
                "Dimension style extension line offset cannot be negative.");
        }

        if (extensionLineOvershoot < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(extensionLineOvershoot),
                extensionLineOvershoot,
                "Dimension style extension line overshoot cannot be negative.");
        }

        if (decimalPlaces < 0 || decimalPlaces > 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decimalPlaces),
                decimalPlaces,
                "Dimension style decimal places must be between 0 and 8.");
        }

        string resolvedSeparator = string.IsNullOrWhiteSpace(decimalSeparator)
            ? "."
            : decimalSeparator.Trim();

        if (resolvedSeparator is not "." and not ",")
        {
            throw new ArgumentException(
                "Dimension style decimal separator must be either '.' or ','.",
                nameof(decimalSeparator));
        }

        Id = id;
        Name = name.Trim();
        TextFormatId = textFormatId;
        ArrowSize = arrowSize;
        TextOffset = textOffset;
        ExtensionLineOffset = extensionLineOffset;
        ExtensionLineOvershoot = extensionLineOvershoot;
        DecimalPlaces = decimalPlaces;
        DecimalSeparator = resolvedSeparator;
        Suffix = suffix ?? string.Empty;
    }

    public DimensionStyleId Id { get; }

    public string Name { get; }

    public TextFormatId TextFormatId { get; }

    public double ArrowSize { get; }

    public double TextOffset { get; }

    public double ExtensionLineOffset { get; }

    public double ExtensionLineOvershoot { get; }

    public int DecimalPlaces { get; }

    public string DecimalSeparator { get; }

    public string Suffix { get; }

    public bool IsBuiltIn => Id == DimensionStyleId.Standard;

    public DimensionStyle WithName(string name)
    {
        return new DimensionStyle(
            Id,
            name,
            TextFormatId,
            ArrowSize,
            TextOffset,
            ExtensionLineOffset,
            ExtensionLineOvershoot,
            DecimalPlaces,
            DecimalSeparator,
            Suffix);
    }

    public DimensionStyle WithGeometry(
        double arrowSize,
        double textOffset,
        double extensionLineOffset,
        double extensionLineOvershoot)
    {
        return new DimensionStyle(
            Id,
            Name,
            TextFormatId,
            arrowSize,
            textOffset,
            extensionLineOffset,
            extensionLineOvershoot,
            DecimalPlaces,
            DecimalSeparator,
            Suffix);
    }

    public DimensionStyle WithText(
        TextFormatId textFormatId,
        int decimalPlaces,
        string decimalSeparator,
        string suffix)
    {
        return new DimensionStyle(
            Id,
            Name,
            textFormatId,
            ArrowSize,
            TextOffset,
            ExtensionLineOffset,
            ExtensionLineOvershoot,
            decimalPlaces,
            decimalSeparator,
            suffix);
    }
}
