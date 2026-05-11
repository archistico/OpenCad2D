using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Layers;

public sealed class LayerManagerWindowViewModel : INotifyPropertyChanged
{
    private EditableLayerViewModel? _selectedLayer;
    private string _validationMessage = string.Empty;

    public LayerManagerWindowViewModel(
        CadDocument document,
        LayerId currentLayerId)
    {
        ArgumentNullException.ThrowIfNull(document);

        LineFormats = document.LineFormats.All
            .OrderBy(format => format.Name)
            .Select(format => new LineFormatOptionViewModel(format))
            .ToList();

        Layers = new ObservableCollection<EditableLayerViewModel>(
            document.Layers.All
                .OrderBy(layer => layer.Name)
                .Select(layer => new EditableLayerViewModel(
                    layer.Id,
                    layer.Name,
                    layer.Id == currentLayerId,
                    layer.IsVisible,
                    layer.IsLocked,
                    layer.LineFormatId,
                    LineFormats,
                    layer.Id == LayerId.Default,
                    document.Entities.All.Count(entity => entity.LayerId == layer.Id))));

        SelectedLayer = Layers.FirstOrDefault(layer => layer.IsCurrent) ?? Layers.FirstOrDefault();
    }

    public IReadOnlyList<LineFormatOptionViewModel> LineFormats { get; }

    public ObservableCollection<EditableLayerViewModel> Layers { get; }

    public EditableLayerViewModel? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (ReferenceEquals(_selectedLayer, value))
            {
                return;
            }

            _selectedLayer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDeleteSelectedLayer));
        }
    }

    public bool CanDeleteSelectedLayer => SelectedLayer?.CanDelete == true;

    public string ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage == value)
            {
                return;
            }

            _validationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidationMessage));
        }
    }

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    public void AddLayer()
    {
        string name = CreateUniqueLayerName();
        var layer = new EditableLayerViewModel(
            new LayerId($"layer-{Guid.NewGuid():N}"),
            name,
            isCurrent: false,
            isVisible: true,
            isLocked: false,
            LineFormatId.Continuous,
            LineFormats,
            isDefault: false,
            entityCount: 0);

        Layers.Add(layer);
        SelectedLayer = layer;
        ClearValidation();
    }

    public void DeleteSelectedLayer()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        if (!SelectedLayer.CanDelete)
        {
            ValidationMessage = "The selected layer cannot be deleted. It may be current, default, or used by entities.";
            return;
        }

        Layers.Remove(SelectedLayer);
        SelectedLayer = Layers.FirstOrDefault(layer => layer.IsCurrent) ?? Layers.FirstOrDefault();
        ClearValidation();
    }

    public void SetCurrentLayer(EditableLayerViewModel layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        foreach (EditableLayerViewModel item in Layers)
        {
            item.IsCurrent = false;
        }

        layer.IsCurrent = true;
        layer.IsVisible = true;
        layer.IsLocked = false;
        SelectedLayer = layer;

        OnPropertyChanged(nameof(CanDeleteSelectedLayer));
        ClearValidation();
    }

    public bool TryBuildResult(out LayerManagerResult result)
    {
        result = new LayerManagerResult(
            Array.Empty<Layer>(),
            LayerId.Default);

        foreach (EditableLayerViewModel layer in Layers)
        {
            string? validation = layer.Validate();

            if (validation is not null)
            {
                ValidationMessage = validation;
                return false;
            }
        }

        var duplicateName = Layers
            .GroupBy(layer => layer.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            ValidationMessage = $"Duplicate layer name '{duplicateName.Key}'.";
            return false;
        }

        EditableLayerViewModel? currentLayer = Layers.FirstOrDefault(layer => layer.IsCurrent);

        if (currentLayer is null)
        {
            ValidationMessage = "A current layer is required.";
            return false;
        }

        if (!currentLayer.IsVisible || currentLayer.IsLocked)
        {
            ValidationMessage = "The current layer must be visible and unlocked.";
            return false;
        }

        List<Layer> layers = Layers
            .Select(layer => layer.ToLayer())
            .ToList();

        result = new LayerManagerResult(
            layers,
            currentLayer.Id);

        ClearValidation();
        return true;
    }

    private string CreateUniqueLayerName()
    {
        int index = 1;

        while (Layers.Any(layer => layer.Name.Equals($"Layer {index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        return $"Layer {index}";
    }

    private void ClearValidation()
    {
        ValidationMessage = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
