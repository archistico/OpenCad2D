using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Layers;

public sealed class EditableLayerViewModel : INotifyPropertyChanged
{
    private string _name;
    private bool _isCurrent;
    private bool _isVisible;
    private bool _isLocked;
    private string _colorHex;
    private string _lineWeightText;

    public EditableLayerViewModel(
        LayerId id,
        string name,
        bool isCurrent,
        bool isVisible,
        bool isLocked,
        CadColor color,
        LineWeight lineWeight,
        bool isDefault,
        int entityCount)
    {
        Id = id;
        _name = name;
        _isCurrent = isCurrent;
        _isVisible = isVisible;
        _isLocked = isLocked;
        _colorHex = ToHex(color);
        _lineWeightText = lineWeight.Millimeters.ToString("0.###", CultureInfo.InvariantCulture);
        IsDefault = isDefault;
        EntityCount = entityCount;
    }

    public LayerId Id { get; }

    public bool IsDefault { get; }

    public int EntityCount { get; }

    public bool CanDelete => !IsDefault && EntityCount == 0 && !IsCurrent;

    public bool CanRename => !IsDefault;

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
        }
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent == value)
            {
                return;
            }

            _isCurrent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDelete));
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked == value)
            {
                return;
            }

            _isLocked = value;
            OnPropertyChanged();
        }
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (_colorHex == value)
            {
                return;
            }

            _colorHex = value;
            OnPropertyChanged();
        }
    }

    public string LineWeightText
    {
        get => _lineWeightText;
        set
        {
            if (_lineWeightText == value)
            {
                return;
            }

            _lineWeightText = value;
            OnPropertyChanged();
        }
    }

    public Layer ToLayer()
    {
        CadColor color = ParseColor(ColorHex);
        LineWeight lineWeight = LineWeight.FromMillimeters(ParseLineWeight(LineWeightText));

        return new Layer(
            Id,
            Name.Trim(),
            color,
            lineWeight,
            IsVisible,
            IsLocked);
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Layer name cannot be empty.";
        }

        if (!TryParseColor(ColorHex, out _))
        {
            return $"Layer '{Name}' has an invalid color. Use #RRGGBB.";
        }

        if (!double.TryParse(LineWeightText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lineWeight) ||
            lineWeight < 0)
        {
            return $"Layer '{Name}' has an invalid line weight.";
        }

        if (IsCurrent && !IsVisible)
        {
            return "The current layer must remain visible.";
        }

        if (IsCurrent && IsLocked)
        {
            return "The current layer must remain unlocked.";
        }

        return null;
    }

    private static string ToHex(CadColor color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static CadColor ParseColor(string text)
    {
        if (!TryParseColor(text, out CadColor color))
        {
            throw new FormatException("Invalid color.");
        }

        return color;
    }

    private static bool TryParseColor(string text, out CadColor color)
    {
        color = CadColor.FromRgb(255, 255, 255);

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string value = text.Trim();

        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length != 6)
        {
            return false;
        }

        if (!byte.TryParse(value[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) ||
            !byte.TryParse(value[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) ||
            !byte.TryParse(value[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
        {
            return false;
        }

        color = CadColor.FromRgb(r, g, b);
        return true;
    }

    private static double ParseLineWeight(string text)
    {
        return double.Parse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
