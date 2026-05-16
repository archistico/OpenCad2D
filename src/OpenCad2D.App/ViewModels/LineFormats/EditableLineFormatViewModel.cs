using Avalonia.Media;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
    private string _patternText;
    private LineStyle _lineStyle;
    private bool _isApplyingPreset;

    public EditableLineFormatViewModel(LineFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);

        Id = format.Id;
        _name = format.Name;
        _colorHex = ToHex(format.Color);
        _lineWeightText = format.LineWeight.Millimeters.ToString("0.###", CultureInfo.InvariantCulture);
        _lineStyle = format.LineStyle;
        _patternText = FormatPattern(format.DashPattern);
        IsBuiltIn = format.IsBuiltIn;
    }

    public LineFormatId Id { get; }

    public bool IsBuiltIn { get; }

    public IReadOnlyList<LineStyle> AvailableLineStyles { get; } = new[]
    {
        LineStyle.Continuous,
        LineStyle.Dashed,
        LineStyle.DashDot,
        LineStyle.DashDotDot,
        LineStyle.Custom
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

            if (!_isApplyingPreset && value != LineStyle.Custom)
            {
                ApplyPresetPattern(value);
            }
        }
    }

    public string PatternText
    {
        get => _patternText;
        set
        {
            string normalized = NormalizePatternText(value);

            if (_patternText == normalized)
            {
                return;
            }

            _patternText = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(PreviewText));

            if (!_isApplyingPreset && LineStyle != LineStyle.Custom)
            {
                _lineStyle = LineStyle.Custom;
                OnPropertyChanged(nameof(LineStyle));
                OnPropertyChanged(nameof(DisplayText));
            }
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

    public string DisplayText => $"{Name} — {LineWeightText} — {LineStyle} — {PatternText}";

    public string PreviewText
    {
        get
        {
            if (!TryParsePattern(PatternText, out IReadOnlyList<double> pattern, out _))
            {
                return "Invalid";
            }

            if (pattern.Count == 0)
            {
                return "────────────";
            }

            return BuildPatternPreview(pattern);
        }
    }

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

        if (!TryParsePattern(PatternText, out _, out string? patternError))
        {
            return $"Line format '{Name}' has an invalid dash pattern. {patternError}";
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
        TryParsePattern(PatternText, out IReadOnlyList<double> dashPattern, out _);

        double lineWeight = double.Parse(
            LineWeightText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

        return new LineFormat(
            Id,
            Name.Trim(),
            color,
            LineWeight.FromMillimeters(lineWeight),
            LineStyle,
            dashPattern);
    }

    private void ApplyPresetPattern(LineStyle style)
    {
        _isApplyingPreset = true;
        try
        {
            PatternText = FormatPattern(LineStyleDashPattern.Get(style));
        }
        finally
        {
            _isApplyingPreset = false;
        }
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

    private static string NormalizePatternText(string? patternText)
    {
        if (string.IsNullOrWhiteSpace(patternText))
        {
            return string.Empty;
        }

        return string.Join(
            ",",
            patternText
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
    }

    private static string FormatPattern(IEnumerable<double>? pattern)
    {
        if (pattern is null)
        {
            return string.Empty;
        }

        return string.Join(
            ",",
            pattern.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)));
    }

    private static bool TryParsePattern(
        string patternText,
        out IReadOnlyList<double> pattern,
        out string? error)
    {
        pattern = Array.Empty<double>();
        error = null;

        if (string.IsNullOrWhiteSpace(patternText))
        {
            return true;
        }

        string[] parts = patternText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length % 2 != 0)
        {
            error = "Use dash/gap pairs, for example 8,4 or 12,4,1,4.";
            return false;
        }

        var values = new List<double>(parts.Length);

        foreach (string part in parts)
        {
            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                error = "Pattern values must be numbers separated by commas.";
                return false;
            }

            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                error = "Pattern values must be positive drawing-unit lengths.";
                return false;
            }

            values.Add(value);
        }

        if (!LineStyleDashPattern.IsValid(values))
        {
            error = "Use positive dash/gap pairs expressed in drawing units.";
            return false;
        }

        pattern = values;
        return true;
    }

    private static string BuildPatternPreview(IReadOnlyList<double> pattern)
    {
        const int targetLength = 14;
        var chars = new List<string>(targetLength);
        int index = 0;

        while (chars.Count < targetLength)
        {
            bool isDash = index % 2 == 0;
            double rawLength = pattern[index % pattern.Count];
            int length = Math.Clamp((int)Math.Round(rawLength / 2.0), 1, 5);
            string token = isDash ? "─" : " ";

            for (int i = 0; i < length && chars.Count < targetLength; i++)
            {
                chars.Add(token);
            }

            index++;
        }

        return string.Concat(chars).TrimEnd();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
