using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.DimensionStyles;

public sealed class DimensionStyleManagerWindowViewModel : INotifyPropertyChanged
{
    private readonly HashSet<DimensionStyleId> _usedDimensionStyleIds;
    private EditableDimensionStyleViewModel? _selectedStyle;
    private EditableDimensionStyleViewModel? _currentStyle;
    private string _validationMessage = string.Empty;

    public DimensionStyleManagerWindowViewModel(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _usedDimensionStyleIds = document.Entities.All
            .OfType<DimensionEntity>()
            .Select(entity => entity.DimensionStyleId)
            .ToHashSet();

        TextFormatOptions = document.TextFormats.All
            .Select(format => new TextFormatOptionViewModel(format))
            .ToList();

        Styles = new ObservableCollection<EditableDimensionStyleViewModel>(
            document.DimensionStyles.All.Select(style => new EditableDimensionStyleViewModel(
                style,
                TextFormatOptions)));

        CurrentStyle = Styles.FirstOrDefault(style => style.Id == document.CurrentDimensionStyleId) ??
                       Styles.FirstOrDefault(style => style.Id == DimensionStyleId.Standard) ??
                       Styles.FirstOrDefault();

        SelectedStyle = CurrentStyle ?? Styles.FirstOrDefault();
    }

    public ObservableCollection<EditableDimensionStyleViewModel> Styles { get; }

    public IReadOnlyList<TextFormatOptionViewModel> TextFormatOptions { get; }

    public EditableDimensionStyleViewModel? SelectedStyle
    {
        get => _selectedStyle;
        set
        {
            if (ReferenceEquals(_selectedStyle, value))
            {
                return;
            }

            _selectedStyle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDeleteSelectedStyle));
            OnPropertyChanged(nameof(CanSetCurrentStyle));
        }
    }

    public EditableDimensionStyleViewModel? CurrentStyle
    {
        get => _currentStyle;
        private set
        {
            if (ReferenceEquals(_currentStyle, value))
            {
                return;
            }

            _currentStyle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentStyleText));
            OnPropertyChanged(nameof(CanDeleteSelectedStyle));
            OnPropertyChanged(nameof(CanSetCurrentStyle));
        }
    }

    public string CurrentStyleText => CurrentStyle is null
        ? "Current: none"
        : $"Current: {CurrentStyle.Name}";

    public bool CanDeleteSelectedStyle =>
        SelectedStyle is not null &&
        !SelectedStyle.IsBuiltIn &&
        !ReferenceEquals(SelectedStyle, CurrentStyle) &&
        !_usedDimensionStyleIds.Contains(SelectedStyle.Id);

    public bool CanSetCurrentStyle =>
        SelectedStyle is not null &&
        !ReferenceEquals(SelectedStyle, CurrentStyle);

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

    public void AddStyle()
    {
        DimensionStyle baseStyle = SelectedStyle?.ToDimensionStyle() ??
                                   Styles.First(style => style.Id == DimensionStyleId.Standard).ToDimensionStyle();

        var style = new EditableDimensionStyleViewModel(
            new DimensionStyle(
                new DimensionStyleId($"dimension-style-{Guid.NewGuid():N}"),
                CreateUniqueStyleName(),
                baseStyle.TextFormatId,
                baseStyle.ArrowSize,
                baseStyle.TextOffset,
                baseStyle.ExtensionLineOffset,
                baseStyle.ExtensionLineOvershoot,
                baseStyle.DecimalPlaces,
                baseStyle.DecimalSeparator,
                baseStyle.Suffix,
                baseStyle.Prefix,
                baseStyle.RadiusPrefix,
                baseStyle.DiameterPrefix,
                baseStyle.ArrowSymbol,
                baseStyle.TextRotationMode,
                baseStyle.DimensionLineOffset),
            TextFormatOptions);

        Styles.Add(style);
        SelectedStyle = style;
        ClearValidation();
    }

    public void DeleteSelectedStyle()
    {
        if (SelectedStyle is null)
        {
            return;
        }

        if (SelectedStyle.IsBuiltIn)
        {
            ValidationMessage = "Built-in dimension styles cannot be deleted.";
            return;
        }

        if (ReferenceEquals(SelectedStyle, CurrentStyle))
        {
            ValidationMessage = "The current dimension style cannot be deleted.";
            return;
        }

        if (_usedDimensionStyleIds.Contains(SelectedStyle.Id))
        {
            ValidationMessage = "This dimension style is used by one or more dimensions and cannot be deleted.";
            return;
        }

        int index = Styles.IndexOf(SelectedStyle);
        Styles.Remove(SelectedStyle);
        SelectedStyle = Styles.Count == 0
            ? null
            : Styles[Math.Clamp(index, 0, Styles.Count - 1)];

        ClearValidation();
        OnPropertyChanged(nameof(CanDeleteSelectedStyle));
    }

    public void SetSelectedAsCurrent()
    {
        if (SelectedStyle is null)
        {
            return;
        }

        CurrentStyle = SelectedStyle;
        ClearValidation();
    }

    public bool TryBuildResult(out DimensionStyleManagerResult result)
    {
        result = new DimensionStyleManagerResult(Array.Empty<DimensionStyle>(), DimensionStyleId.Standard);

        if (Styles.Count == 0)
        {
            ValidationMessage = "At least one dimension style is required.";
            return false;
        }

        foreach (EditableDimensionStyleViewModel style in Styles)
        {
            string? validation = style.Validate();
            if (validation is not null)
            {
                ValidationMessage = validation;
                return false;
            }
        }

        var duplicateName = Styles
            .GroupBy(style => style.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            ValidationMessage = $"Duplicate dimension style name '{duplicateName.Key}'.";
            return false;
        }

        IReadOnlyList<DimensionStyle> dimensionStyles = Styles
            .Select(style => style.ToDimensionStyle())
            .ToList();

        if (!dimensionStyles.Any(style => style.Id == DimensionStyleId.Standard))
        {
            ValidationMessage = "The Standard dimension style is required.";
            return false;
        }

        if (CurrentStyle is null || !dimensionStyles.Any(style => style.Id == CurrentStyle.Id))
        {
            ValidationMessage = "A current dimension style is required.";
            return false;
        }

        result = new DimensionStyleManagerResult(dimensionStyles, CurrentStyle.Id);
        ClearValidation();
        return true;
    }

    private string CreateUniqueStyleName()
    {
        const string baseName = "New dimension style";

        if (!Styles.Any(style => style.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase)))
        {
            return baseName;
        }

        int index = 2;
        while (Styles.Any(style => style.Name.Equals($"{baseName} {index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        return $"{baseName} {index}";
    }

    private void ClearValidation()
    {
        ValidationMessage = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
