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
        string suffix = "",
        string prefix = "",
        string radiusPrefix = "R ",
        string diameterPrefix = "Ø ",
        DimensionArrowSymbol arrowSymbol = DimensionArrowSymbol.ClosedArrow,
        DimensionTextRotationMode textRotationMode = DimensionTextRotationMode.Readable,
        double dimensionLineOffset = 8.0,
        DimensionTextFitMode textFitMode = DimensionTextFitMode.OutsideWhenNeeded,
        DimensionTerminatorFitMode terminatorFitMode = DimensionTerminatorFitMode.OutsideWhenNeeded)
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

        if (dimensionLineOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dimensionLineOffset),
                dimensionLineOffset,
                "Dimension style dimension line offset cannot be negative.");
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
        Prefix = prefix ?? string.Empty;
        Suffix = suffix ?? string.Empty;
        RadiusPrefix = radiusPrefix ?? string.Empty;
        DiameterPrefix = diameterPrefix ?? string.Empty;
        ArrowSymbol = arrowSymbol;
        TextRotationMode = textRotationMode;
        DimensionLineOffset = dimensionLineOffset;
        TextFitMode = textFitMode;
        TerminatorFitMode = terminatorFitMode;
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

    public string Prefix { get; }

    public string Suffix { get; }

    public string RadiusPrefix { get; }

    public string DiameterPrefix { get; }

    public DimensionArrowSymbol ArrowSymbol { get; }

    public DimensionTextRotationMode TextRotationMode { get; }

    /// <summary>
    /// Preferred default distance between measured points and a dimension line.
    /// Interactive tools may still override this by using the explicitly picked dimension line point.
    /// </summary>
    public double DimensionLineOffset { get; }

    /// <summary>
    /// Controls whether dimension text stays inside the measured span or moves outside when space is tight.
    /// </summary>
    public DimensionTextFitMode TextFitMode { get; }

    /// <summary>
    /// Controls whether dimension terminators stay inside the measured span or move outside when space is tight.
    /// </summary>
    public DimensionTerminatorFitMode TerminatorFitMode { get; }

    public bool IsBuiltIn =>
        Id == DimensionStyleId.Standard ||
        Id == DimensionStyleId.Architectural ||
        Id == DimensionStyleId.Mechanical;

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
            Suffix,
            Prefix,
            RadiusPrefix,
            DiameterPrefix,
            ArrowSymbol,
            TextRotationMode,
            DimensionLineOffset,
            TextFitMode,
            TerminatorFitMode);
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
            Suffix,
            Prefix,
            RadiusPrefix,
            DiameterPrefix,
            ArrowSymbol,
            TextRotationMode,
            DimensionLineOffset,
            TextFitMode,
            TerminatorFitMode);
    }

    public DimensionStyle WithText(
        TextFormatId textFormatId,
        int decimalPlaces,
        string decimalSeparator,
        string suffix,
        string? prefix = null,
        string? radiusPrefix = null,
        string? diameterPrefix = null)
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
            suffix,
            prefix ?? Prefix,
            radiusPrefix ?? RadiusPrefix,
            diameterPrefix ?? DiameterPrefix,
            ArrowSymbol,
            TextRotationMode,
            DimensionLineOffset,
            TextFitMode,
            TerminatorFitMode);
    }

    public DimensionStyle WithSymbols(
        DimensionArrowSymbol arrowSymbol,
        double arrowSize)
    {
        return new DimensionStyle(
            Id,
            Name,
            TextFormatId,
            arrowSize,
            TextOffset,
            ExtensionLineOffset,
            ExtensionLineOvershoot,
            DecimalPlaces,
            DecimalSeparator,
            Suffix,
            Prefix,
            RadiusPrefix,
            DiameterPrefix,
            arrowSymbol,
            TextRotationMode,
            DimensionLineOffset,
            TextFitMode,
            TerminatorFitMode);
    }

    public DimensionStyle WithOrientation(DimensionTextRotationMode textRotationMode)
    {
        return new DimensionStyle(
            Id,
            Name,
            TextFormatId,
            ArrowSize,
            TextOffset,
            ExtensionLineOffset,
            ExtensionLineOvershoot,
            DecimalPlaces,
            DecimalSeparator,
            Suffix,
            Prefix,
            RadiusPrefix,
            DiameterPrefix,
            ArrowSymbol,
            textRotationMode,
            DimensionLineOffset,
            TextFitMode,
            TerminatorFitMode);
    }

    public DimensionStyle WithDimensionLineOffset(double dimensionLineOffset)
    {
        return new DimensionStyle(
            Id,
            Name,
            TextFormatId,
            ArrowSize,
            TextOffset,
            ExtensionLineOffset,
            ExtensionLineOvershoot,
            DecimalPlaces,
            DecimalSeparator,
            Suffix,
            Prefix,
            RadiusPrefix,
            DiameterPrefix,
            ArrowSymbol,
            TextRotationMode,
            dimensionLineOffset,
            TextFitMode,
            TerminatorFitMode);
    }

    public DimensionStyle WithTextFitMode(DimensionTextFitMode textFitMode)
    {
        return new DimensionStyle(
            Id,
            Name,
            TextFormatId,
            ArrowSize,
            TextOffset,
            ExtensionLineOffset,
            ExtensionLineOvershoot,
            DecimalPlaces,
            DecimalSeparator,
            Suffix,
            Prefix,
            RadiusPrefix,
            DiameterPrefix,
            ArrowSymbol,
            TextRotationMode,
            DimensionLineOffset,
            textFitMode,
            TerminatorFitMode);
    }


    public DimensionStyle WithTerminatorFitMode(DimensionTerminatorFitMode terminatorFitMode)
    {
        return new DimensionStyle(
            Id,
            Name,
            TextFormatId,
            ArrowSize,
            TextOffset,
            ExtensionLineOffset,
            ExtensionLineOvershoot,
            DecimalPlaces,
            DecimalSeparator,
            Suffix,
            Prefix,
            RadiusPrefix,
            DiameterPrefix,
            ArrowSymbol,
            TextRotationMode,
            DimensionLineOffset,
            TextFitMode,
            terminatorFitMode);
    }


}
