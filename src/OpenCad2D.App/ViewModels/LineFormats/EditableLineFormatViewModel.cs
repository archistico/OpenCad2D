using Avalonia.Media;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace OpenCad2D.App.ViewModels.LineFormats;

public sealed class EditableLineFormatViewModel : INotifyPropertyChanged
{
    private static readonly Regex ColorHexRegex = new(
        "^#[0-9A-Fa-f]{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private string _name;
    private string _colorHex;
    private string _lineWeightText;
    private LineStyle _lineStyle;

    public EditableLineFormatViewModel(LineFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);

        Id = format.Id;
        _name = format.Name;
        _colorHex = ToHex(format.Color);
        _lineWeightText = format.LineWeight.Millimeters.ToString("0.###", CultureInfo.InvariantCulture);
        _lineStyle = format.LineStyle;
        IsBuiltIn = format.IsBuiltIn;
    }

    public LineFormatId Id { get; }

    public bool IsBuiltIn { get; }

    public IReadOnlyList<LineStyle> AvailableLineStyles { get; } = new[]
    {
        LineStyle.Continuous,
        LineStyle.Dashed,
        LineStyle.DashDot,
        LineStyle.DashDotDot
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
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(ColorBrush));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public Color Color
    {
        get
        {
            if (!TryParseColor(ColorHex, out CadColor color))
            {
                color = CadColor.FromRgb(80, 80, 80);
            }

            return Color.FromRgb(color.R, color.G, color.B);
        }
        set
        {
            ColorHex = ToHex(CadColor.FromRgb(value.R, value.G, value.B));
        }
    }

    public string LineWeightText
    {
        get => _lineWeightText;
        set
        {
            string normalized = value?.Trim() ?? string.Empty;

            if (_lineWeightText == normalized)
            {
                return;
            }

            _lineWeightText = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public LineStyle LineStyle
    {
        get => _lineStyle;
        set
        {
            if (_lineStyle == value)
            {
                return;
            }

            _lineStyle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(PreviewText));
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

    public string BuiltInText => IsBuiltIn ? "Built-in" : string.Empty;

    public string DisplayText => $"{Name} — {LineWeightText} — {LineStyle}";

    public string PreviewText => LineStyle switch
    {
        LineStyle.Continuous => "────────",
        LineStyle.Dashed => "─ ─ ─ ─",
        LineStyle.DashDot => "─ · ─ ·",
        LineStyle.DashDotDot => "─ · · ─",
        _ => "────────"
    };

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Line format name cannot be empty.";
        }

        if (!TryParseColor(ColorHex, out _))
        {
            return $"Line format '{Name}' has an invalid color. Use #RRGGBB.";
        }

        if (!double.TryParse(
                LineWeightText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double lineWeight))
        {
            return $"Line format '{Name}' has an invalid line weight.";
        }

        if (lineWeight < 0)
        {
            return $"Line format '{Name}' has a negative line weight.";
        }

        return null;
    }

    public LineFormat ToLineFormat()
    {
        string? validation = Validate();

        if (validation is not null)
        {
            throw new InvalidOperationException(validation);
        }

        TryParseColor(ColorHex, out CadColor color);

        double lineWeight = double.Parse(
            LineWeightText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

        return new LineFormat(
            Id,
            Name.Trim(),
            color,
            LineWeight.FromMillimeters(lineWeight),
            LineStyle);
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
