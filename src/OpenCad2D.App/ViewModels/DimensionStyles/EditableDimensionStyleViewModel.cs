using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Identifiers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.DimensionStyles;

public sealed class EditableDimensionStyleViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<TextFormatOptionViewModel> _textFormatOptions;
    private string _name;
    private TextFormatOptionViewModel? _selectedTextFormat;
    private string _arrowSizeText;
    private string _textOffsetText;
    private string _extensionLineOffsetText;
    private string _extensionLineOvershootText;
    private string _dimensionLineOffsetText;
    private string _decimalPlacesText;
    private string _decimalSeparator;
    private string _prefix;
    private string _suffix;
    private string _radiusPrefix;
    private string _diameterPrefix;
    private DimensionArrowSymbol _arrowSymbol;
    private DimensionTextRotationMode _textRotationMode;
    private DimensionTextFitMode _textFitMode;
    private DimensionTerminatorFitMode _terminatorFitMode;

    public EditableDimensionStyleViewModel(
        DimensionStyle style,
        IReadOnlyList<TextFormatOptionViewModel> textFormatOptions)
    {
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(textFormatOptions);

        Id = style.Id;
        IsBuiltIn = style.IsBuiltIn;
        _textFormatOptions = textFormatOptions;
        _name = style.Name;
        _selectedTextFormat = textFormatOptions.FirstOrDefault(option => option.Id == style.TextFormatId) ??
                              textFormatOptions.FirstOrDefault();
        _arrowSizeText = ToText(style.ArrowSize);
        _textOffsetText = ToText(style.TextOffset);
        _extensionLineOffsetText = ToText(style.ExtensionLineOffset);
        _extensionLineOvershootText = ToText(style.ExtensionLineOvershoot);
        _dimensionLineOffsetText = ToText(style.DimensionLineOffset);
        _decimalPlacesText = style.DecimalPlaces.ToString(CultureInfo.InvariantCulture);
        _decimalSeparator = style.DecimalSeparator;
        _prefix = style.Prefix;
        _suffix = style.Suffix;
        _radiusPrefix = style.RadiusPrefix;
        _diameterPrefix = style.DiameterPrefix;
        _arrowSymbol = style.ArrowSymbol;
        _textRotationMode = style.TextRotationMode;
        _textFitMode = style.TextFitMode;
        _terminatorFitMode = style.TerminatorFitMode;
    }

    public DimensionStyleId Id { get; }

    public bool IsBuiltIn { get; }

    public IReadOnlyList<TextFormatOptionViewModel> TextFormatOptions => _textFormatOptions;

    public IReadOnlyList<DimensionArrowSymbol> ArrowSymbols { get; } =
        Enum.GetValues<DimensionArrowSymbol>();

    public IReadOnlyList<DimensionTextRotationMode> TextRotationModes { get; } =
        Enum.GetValues<DimensionTextRotationMode>();

    public IReadOnlyList<DimensionTextFitMode> TextFitModes { get; } =
        Enum.GetValues<DimensionTextFitMode>();

    public IReadOnlyList<DimensionTerminatorFitMode> TerminatorFitModes { get; } =
        Enum.GetValues<DimensionTerminatorFitMode>();

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value ?? string.Empty);
    }

    public TextFormatOptionViewModel? SelectedTextFormat
    {
        get => _selectedTextFormat;
        set => SetField(ref _selectedTextFormat, value);
    }

    public string ArrowSizeText
    {
        get => _arrowSizeText;
        set => SetField(ref _arrowSizeText, NormalizeText(value));
    }

    public string TextOffsetText
    {
        get => _textOffsetText;
        set => SetField(ref _textOffsetText, NormalizeText(value));
    }

    public string ExtensionLineOffsetText
    {
        get => _extensionLineOffsetText;
        set => SetField(ref _extensionLineOffsetText, NormalizeText(value));
    }

    public string ExtensionLineOvershootText
    {
        get => _extensionLineOvershootText;
        set => SetField(ref _extensionLineOvershootText, NormalizeText(value));
    }

    public string DimensionLineOffsetText
    {
        get => _dimensionLineOffsetText;
        set => SetField(ref _dimensionLineOffsetText, NormalizeText(value));
    }

    public string DecimalPlacesText
    {
        get => _decimalPlacesText;
        set => SetField(ref _decimalPlacesText, NormalizeText(value));
    }

    public string DecimalSeparator
    {
        get => _decimalSeparator;
        set => SetField(ref _decimalSeparator, NormalizeText(value));
    }

    public string Prefix
    {
        get => _prefix;
        set => SetField(ref _prefix, value ?? string.Empty);
    }

    public string Suffix
    {
        get => _suffix;
        set => SetField(ref _suffix, value ?? string.Empty);
    }

    public string RadiusPrefix
    {
        get => _radiusPrefix;
        set => SetField(ref _radiusPrefix, value ?? string.Empty);
    }

    public string DiameterPrefix
    {
        get => _diameterPrefix;
        set => SetField(ref _diameterPrefix, value ?? string.Empty);
    }

    public DimensionArrowSymbol ArrowSymbol
    {
        get => _arrowSymbol;
        set => SetField(ref _arrowSymbol, value);
    }

    public DimensionTextRotationMode TextRotationMode
    {
        get => _textRotationMode;
        set => SetField(ref _textRotationMode, value);
    }

    public DimensionTextFitMode TextFitMode
    {
        get => _textFitMode;
        set => SetField(ref _textFitMode, value);
    }

    public DimensionTerminatorFitMode TerminatorFitMode
    {
        get => _terminatorFitMode;
        set => SetField(ref _terminatorFitMode, value);
    }

    public string BuiltInText => IsBuiltIn ? "Built-in" : string.Empty;

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Dimension style name cannot be empty.";
        }

        if (SelectedTextFormat is null)
        {
            return $"Dimension style '{Name}' requires a text format.";
        }

        if (!TryParsePositive(ArrowSizeText, out _))
        {
            return $"Dimension style '{Name}' has an invalid arrow size.";
        }

        if (!TryParseDouble(TextOffsetText, out _))
        {
            return $"Dimension style '{Name}' has an invalid text offset.";
        }

        if (!TryParseNonNegative(ExtensionLineOffsetText, out _))
        {
            return $"Dimension style '{Name}' has an invalid extension line offset.";
        }

        if (!TryParseNonNegative(ExtensionLineOvershootText, out _))
        {
            return $"Dimension style '{Name}' has an invalid extension line overshoot.";
        }

        if (!TryParseNonNegative(DimensionLineOffsetText, out _))
        {
            return $"Dimension style '{Name}' has an invalid dimension line offset.";
        }

        if (!int.TryParse(DecimalPlacesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int decimalPlaces) ||
            decimalPlaces < 0 ||
            decimalPlaces > 8)
        {
            return $"Dimension style '{Name}' decimal places must be between 0 and 8.";
        }

        string separator = DecimalSeparator.Trim();
        if (separator is not "." and not ",")
        {
            return $"Dimension style '{Name}' decimal separator must be '.' or ','.";
        }

        return null;
    }

    public DimensionStyle ToDimensionStyle()
    {
        string? validation = Validate();
        if (validation is not null)
        {
            throw new InvalidOperationException(validation);
        }

        double arrowSize = ParseDouble(ArrowSizeText);
        double textOffset = ParseDouble(TextOffsetText);
        double extensionLineOffset = ParseDouble(ExtensionLineOffsetText);
        double extensionLineOvershoot = ParseDouble(ExtensionLineOvershootText);
        double dimensionLineOffset = ParseDouble(DimensionLineOffsetText);
        int decimalPlaces = int.Parse(DecimalPlacesText, NumberStyles.Integer, CultureInfo.InvariantCulture);

        return new DimensionStyle(
            Id,
            Name.Trim(),
            SelectedTextFormat!.Id,
            arrowSize,
            textOffset,
            extensionLineOffset,
            extensionLineOvershoot,
            decimalPlaces,
            DecimalSeparator.Trim(),
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

    private static string ToText(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string NormalizeText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static bool TryParsePositive(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result > 0;
    }

    private static bool TryParseNonNegative(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result >= 0;
    }

    private static bool TryParseDouble(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private static double ParseDouble(string value)
    {
        return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
