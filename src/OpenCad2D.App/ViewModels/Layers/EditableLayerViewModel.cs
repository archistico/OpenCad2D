using Avalonia.Media;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace OpenCad2D.App.ViewModels.Layers;

public sealed class EditableLayerViewModel : INotifyPropertyChanged
{
    private static readonly Regex ColorHexRegex = new(
        "^#[0-9A-Fa-f]{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private string _name;
    private bool _isCurrent;
    private bool _isVisible;
    private bool _isLocked;
    private string _fillColorHex;
    private LineFormatOptionViewModel _selectedLineFormat;

    public EditableLayerViewModel(
        LayerId id,
        string name,
        bool isCurrent,
        bool isVisible,
        bool isLocked,
        LineFormatId selectedLineFormatId,
        IReadOnlyList<LineFormatOptionViewModel> availableLineFormats,
        bool isDefault,
        int entityCount,
        CadColor? fillColor = null)
    {
        ArgumentNullException.ThrowIfNull(availableLineFormats);

        if (availableLineFormats.Count == 0)
        {
            throw new ArgumentException(
                "At least one line format is required.",
                nameof(availableLineFormats));
        }

        Id = id;
        _name = name;
        _isCurrent = isCurrent;
        _isVisible = isVisible;
        _isLocked = isLocked;
        _fillColorHex = ToHex(fillColor ?? CadColor.FromRgb(255, 255, 255));
        AvailableLineFormats = availableLineFormats;
        _selectedLineFormat = availableLineFormats.FirstOrDefault(format => format.Id == selectedLineFormatId) ??
                              availableLineFormats.FirstOrDefault(format => format.Id == LineFormatId.Continuous) ??
                              availableLineFormats[0];
        IsDefault = isDefault;
        EntityCount = entityCount;
    }

    public LayerId Id { get; }

    public bool IsDefault { get; }

    public int EntityCount { get; }

    public bool CanDelete => !IsDefault && EntityCount == 0 && !IsCurrent;

    public bool CanRename => !IsDefault;

    public IReadOnlyList<LineFormatOptionViewModel> AvailableLineFormats { get; }

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


    public string FillColorHex
    {
        get => _fillColorHex;
        set
        {
            string normalized = value?.Trim() ?? string.Empty;

            if (_fillColorHex == normalized)
            {
                return;
            }

            _fillColorHex = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FillColor));
            OnPropertyChanged(nameof(FillColorBrush));
        }
    }

    public Color FillColor
    {
        get
        {
            if (!TryParseColor(FillColorHex, out CadColor color))
            {
                color = CadColor.FromRgb(80, 80, 80);
            }

            return Color.FromRgb(color.R, color.G, color.B);
        }
        set
        {
            FillColorHex = ToHex(CadColor.FromRgb(value.R, value.G, value.B));
        }
    }

    public IBrush FillColorBrush
    {
        get
        {
            if (!TryParseColor(FillColorHex, out CadColor color))
            {
                color = CadColor.FromRgb(80, 80, 80);
            }

            return new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        }
    }

    public LineFormatOptionViewModel SelectedLineFormat
    {
        get => _selectedLineFormat;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(_selectedLineFormat, value) ||
                _selectedLineFormat.Id == value.Id)
            {
                return;
            }

            _selectedLineFormat = value;
            OnPropertyChanged();
        }
    }

    public Layer ToLayer()
    {
        TryParseColor(FillColorHex, out CadColor fillColor);

        return new Layer(
            Id,
            Name.Trim(),
            SelectedLineFormat.Id,
            IsVisible,
            IsLocked,
            fillColor);
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Layer name cannot be empty.";
        }

        if (!TryParseColor(FillColorHex, out _))
        {
            return $"Layer '{Name}' has an invalid fill color. Use #RRGGBB.";
        }

        if (SelectedLineFormat is null)
        {
            return $"Layer '{Name}' must have a line format.";
        }

        if (AvailableLineFormats.All(format => format.Id != SelectedLineFormat.Id))
        {
            return $"Layer '{Name}' references an unknown line format.";
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
