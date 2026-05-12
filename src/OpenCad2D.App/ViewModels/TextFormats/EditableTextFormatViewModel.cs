using Avalonia.Media;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace OpenCad2D.App.ViewModels.TextFormats;

public sealed class EditableTextFormatViewModel : INotifyPropertyChanged
{
    private static readonly Regex ColorHexRegex = new(
        "^#[0-9A-Fa-f]{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private string _name;
    private string _fontFamily;
    private string _heightText;
    private string _colorHex;
    private bool _isBold;
    private bool _isItalic;

    public EditableTextFormatViewModel(TextFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);

        Id = format.Id;
        _name = format.Name;
        _fontFamily = format.FontFamily;
        _heightText = format.Height.ToString("0.###", CultureInfo.InvariantCulture);
        _colorHex = ToHex(format.Color);
        _isBold = format.IsBold;
        _isItalic = format.IsItalic;
        IsBuiltIn = format.IsBuiltIn;
    }

    public TextFormatId Id { get; }

    public bool IsBuiltIn { get; }

    public IReadOnlyList<string> CommonFontFamilies { get; } = new[]
    {
        "Arial",
        "Segoe UI",
        "Inter",
        "Calibri",
        "Consolas",
        "Tahoma",
        "Times New Roman"
    };

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            string normalized = value?.Trim() ?? string.Empty;

            if (_fontFamily == normalized)
            {
                return;
            }

            _fontFamily = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewFontFamily));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string HeightText
    {
        get => _heightText;
        set
        {
            string normalized = value?.Trim() ?? string.Empty;

            if (_heightText == normalized)
            {
                return;
            }

            _heightText = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewFontSize));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            string normalized = value?.Trim() ?? string.Empty;

            if (_colorHex == normalized)
            {
                return;
            }

            _colorHex = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorBrush));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public bool IsBold
    {
        get => _isBold;
        set
        {
            if (_isBold == value)
            {
                return;
            }

            _isBold = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewFontWeight));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public bool IsItalic
    {
        get => _isItalic;
        set
        {
            if (_isItalic == value)
            {
                return;
            }

            _isItalic = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewFontStyle));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public IBrush ColorBrush
    {
        get
        {
            if (!TryParseColor(ColorHex, out CadColor color))
            {
                color = CadColor.FromRgb(80, 80, 80);
            }

            return new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        }
    }

    public FontFamily PreviewFontFamily => new(FontFamily);

    public double PreviewFontSize
    {
        get
        {
            if (!double.TryParse(
                    HeightText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double height))
            {
                return 14;
            }

            return Math.Clamp(height * 1.7, 11, 28);
        }
    }

    public FontWeight PreviewFontWeight => IsBold
        ? FontWeight.Bold
        : FontWeight.Normal;

    public FontStyle PreviewFontStyle => IsItalic
        ? FontStyle.Italic
        : FontStyle.Normal;

    public string BuiltInText => IsBuiltIn ? "Built-in" : string.Empty;

    public string DisplayText => $"{Name} — {FontFamily} — {HeightText}";

    public string PreviewText => "Sample Text";

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Text format name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(FontFamily))
        {
            return $"Text format '{Name}' has an empty font family.";
        }

        if (!double.TryParse(
                HeightText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double height))
        {
            return $"Text format '{Name}' has an invalid height.";
        }

        if (height <= 0)
        {
            return $"Text format '{Name}' height must be greater than zero.";
        }

        if (!TryParseColor(ColorHex, out _))
        {
            return $"Text format '{Name}' has an invalid color. Use #RRGGBB.";
        }

        return null;
    }

    public TextFormat ToTextFormat()
    {
        string? validation = Validate();

        if (validation is not null)
        {
            throw new InvalidOperationException(validation);
        }

        TryParseColor(ColorHex, out CadColor color);

        double height = double.Parse(
            HeightText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

        return new TextFormat(
            Id,
            Name.Trim(),
            FontFamily.Trim(),
            height,
            color,
            IsBold,
            IsItalic);
    }

    private static string ToHex(CadColor color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static bool TryParseColor(
        string colorHex,
        out CadColor color)
    {
        color = CadColor.FromRgb(0, 0, 0);

        if (!ColorHexRegex.IsMatch(colorHex))
        {
            return false;
        }

        byte r = byte.Parse(colorHex.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte g = byte.Parse(colorHex.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte b = byte.Parse(colorHex.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        color = CadColor.FromRgb(r, g, b);
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
