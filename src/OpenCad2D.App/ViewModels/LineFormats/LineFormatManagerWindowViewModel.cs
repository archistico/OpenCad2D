using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.LineFormats;

public sealed class LineFormatManagerWindowViewModel : INotifyPropertyChanged
{
    private readonly HashSet<LineFormatId> _usedLineFormatIds;
    private EditableLineFormatViewModel? _selectedFormat;
    private string _validationMessage = string.Empty;

    public LineFormatManagerWindowViewModel(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _usedLineFormatIds = document.Layers.All
            .Select(layer => layer.LineFormatId)
            .ToHashSet();

        Formats = new ObservableCollection<EditableLineFormatViewModel>(
            document.LineFormats.All.Select(format => new EditableLineFormatViewModel(format)));

        SelectedFormat = Formats.FirstOrDefault();
    }

    public ObservableCollection<EditableLineFormatViewModel> Formats { get; }

    public EditableLineFormatViewModel? SelectedFormat
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
        !_usedLineFormatIds.Contains(SelectedFormat.Id);

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
        var format = new EditableLineFormatViewModel(
            new LineFormat(
                new LineFormatId($"line-format-{Guid.NewGuid():N}"),
                CreateUniqueFormatName(),
                CadColor.FromRgb(255, 255, 255),
                LineWeight.FromMillimeters(1.0),
                LineStyle.Continuous));

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
            ValidationMessage = "Built-in line formats cannot be deleted.";
            return;
        }

        if (_usedLineFormatIds.Contains(SelectedFormat.Id))
        {
            ValidationMessage = "This line format is used by one or more layers and cannot be deleted.";
            return;
        }

        int index = Formats.IndexOf(SelectedFormat);
        Formats.Remove(SelectedFormat);

        SelectedFormat = Formats.Count == 0
            ? null
            : Formats[Math.Clamp(index, 0, Formats.Count - 1)];

        ClearValidation();
    }

    public bool TryBuildResult(out LineFormatManagerResult result)
    {
        result = new LineFormatManagerResult(Array.Empty<LineFormat>());

        if (Formats.Count == 0)
        {
            ValidationMessage = "At least one line format is required.";
            return false;
        }

        foreach (EditableLineFormatViewModel format in Formats)
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
            ValidationMessage = $"Duplicate line format name '{duplicateName.Key}'.";
            return false;
        }

        IReadOnlyList<LineFormat> lineFormats = Formats
            .Select(format => format.ToLineFormat())
            .ToList();

        if (!lineFormats.Any(format => format.Id == LineFormatId.Continuous))
        {
            ValidationMessage = "The Continuous line format is required.";
            return false;
        }

        result = new LineFormatManagerResult(lineFormats);
        ClearValidation();
        return true;
    }

    private string CreateUniqueFormatName()
    {
        const string baseName = "Nuovo formato";

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
