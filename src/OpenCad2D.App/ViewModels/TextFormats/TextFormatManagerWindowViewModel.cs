using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.TextFormats;

public sealed class TextFormatManagerWindowViewModel : INotifyPropertyChanged
{
    private readonly HashSet<TextFormatId> _usedTextFormatIds;
    private EditableTextFormatViewModel? _selectedFormat;
    private string _validationMessage = string.Empty;

    public TextFormatManagerWindowViewModel(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _usedTextFormatIds = document.Entities.All
            .OfType<TextEntity>()
            .Select(entity => entity.TextFormatId)
            .ToHashSet();

        Formats = new ObservableCollection<EditableTextFormatViewModel>(
            document.TextFormats.All.Select(format => new EditableTextFormatViewModel(format)));

        SelectedFormat = Formats.FirstOrDefault();
    }

    public ObservableCollection<EditableTextFormatViewModel> Formats { get; }

    public EditableTextFormatViewModel? SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (ReferenceEquals(_selectedFormat, value))
            {
                return;
            }

            _selectedFormat = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDeleteSelectedFormat));
        }
    }

    public bool CanDeleteSelectedFormat =>
        SelectedFormat is not null &&
        !SelectedFormat.IsBuiltIn &&
        !_usedTextFormatIds.Contains(SelectedFormat.Id);

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

    public void AddFormat()
    {
        var format = new EditableTextFormatViewModel(
            new TextFormat(
                new TextFormatId($"text-format-{Guid.NewGuid():N}"),
                CreateUniqueFormatName(),
                "Arial",
                10.0,
                CadColor.FromRgb(255, 255, 255)));

        Formats.Add(format);
        SelectedFormat = format;
        ClearValidation();
    }

    public void DeleteSelectedFormat()
    {
        if (SelectedFormat is null)
        {
            return;
        }

        if (SelectedFormat.IsBuiltIn)
        {
            ValidationMessage = "Built-in text formats cannot be deleted.";
            return;
        }

        if (_usedTextFormatIds.Contains(SelectedFormat.Id))
        {
            ValidationMessage = "This text format is used by one or more text entities and cannot be deleted.";
            return;
        }

        int index = Formats.IndexOf(SelectedFormat);
        Formats.Remove(SelectedFormat);

        SelectedFormat = Formats.Count == 0
            ? null
            : Formats[Math.Clamp(index, 0, Formats.Count - 1)];

        ClearValidation();
    }

    public bool TryBuildResult(out TextFormatManagerResult result)
    {
        result = new TextFormatManagerResult(Array.Empty<TextFormat>());

        if (Formats.Count == 0)
        {
            ValidationMessage = "At least one text format is required.";
            return false;
        }

        foreach (EditableTextFormatViewModel format in Formats)
        {
            string? validation = format.Validate();

            if (validation is not null)
            {
                ValidationMessage = validation;
                return false;
            }
        }

        var duplicateName = Formats
            .GroupBy(format => format.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            ValidationMessage = $"Duplicate text format name '{duplicateName.Key}'.";
            return false;
        }

        IReadOnlyList<TextFormat> textFormats = Formats
            .Select(format => format.ToTextFormat())
            .ToList();

        if (!textFormats.Any(format => format.Id == TextFormatId.Standard))
        {
            ValidationMessage = "The Standard text format is required.";
            return false;
        }

        result = new TextFormatManagerResult(textFormats);
        ClearValidation();
        return true;
    }

    private string CreateUniqueFormatName()
    {
        const string baseName = "Nuovo formato testo";

        if (!Formats.Any(format => format.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase)))
        {
            return baseName;
        }

        int index = 2;

        while (Formats.Any(format => format.Name.Equals($"{baseName} {index}", StringComparison.OrdinalIgnoreCase)))
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
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
