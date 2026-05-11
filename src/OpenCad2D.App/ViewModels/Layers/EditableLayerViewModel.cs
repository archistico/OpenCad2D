using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Layers;

public sealed class EditableLayerViewModel : INotifyPropertyChanged
{
    private string _name;
    private bool _isCurrent;
    private bool _isVisible;
    private bool _isLocked;
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
        int entityCount)
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
        return new Layer(
            Id,
            Name.Trim(),
            SelectedLineFormat.Id,
            IsVisible,
            IsLocked);
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Layer name cannot be empty.";
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
