using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Blocks;

public sealed class EditableBlockDefinitionViewModel : INotifyPropertyChanged
{
    private string _name;

    public EditableBlockDefinitionViewModel(
        BlockDefinition definition,
        int instanceCount)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        Id = definition.Id;
        _name = definition.Name;
        EntityCount = definition.Entities.Count;
        InstanceCount = instanceCount;
        BoundsText = FormatBounds(definition.GetBoundingBox());
    }

    public BlockDefinition Definition { get; }

    public BlockDefinitionId Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            string nextValue = value ?? string.Empty;

            if (_name == nextValue)
            {
                return;
            }

            _name = nextValue;
            OnPropertyChanged();
        }
    }

    public int EntityCount { get; }

    public int InstanceCount { get; }

    public string EntityCountText => EntityCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string InstanceCountText => InstanceCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string BoundsText { get; }

    public bool CanDelete => InstanceCount == 0;

    public string StatusText => CanDelete ? "Unused" : "Used";

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Block name cannot be empty.";
        }

        return null;
    }

    public BlockDefinition ToBlockDefinition()
    {
        return Definition.WithName(Name.Trim());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatBounds(BoundingBox2D bounds)
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{bounds.Width:0.###} × {bounds.Height:0.###}");
    }
}
