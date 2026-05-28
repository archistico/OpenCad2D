using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.ImageReferences;

public sealed class ImageReferenceManagerWindowViewModel : INotifyPropertyChanged
{
    private ImageReferenceItemViewModel? _selectedReference;
    private string _transparencyPercentText = "0";

    public ImageReferenceManagerWindowViewModel(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        References = new ObservableCollection<ImageReferenceItemViewModel>(
            document.Entities.All
                .OfType<ImageReferenceEntity>()
                .GroupBy(imageReference => imageReference.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ImageReferenceItemViewModel(
                    group.First(),
                    group.Count()))
                .OrderByDescending(item => item.IsMissing)
                .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase));

        SelectedReference = References.FirstOrDefault();
    }

    public ObservableCollection<ImageReferenceItemViewModel> References { get; }

    public ImageReferenceItemViewModel? SelectedReference
    {
        get => _selectedReference;
        set
        {
            if (ReferenceEquals(_selectedReference, value))
            {
                return;
            }

            _selectedReference = value;
            _transparencyPercentText = value is null
                ? string.Empty
                : value.TransparencyPercent.ToString("0.#", CultureInfo.InvariantCulture);
            OnPropertyChanged();
            OnPropertyChanged(nameof(TransparencyPercentText));
            OnPropertyChanged(nameof(HasSelectedReference));
            OnPropertyChanged(nameof(CanOpenSelectedFolder));
            OnPropertyChanged(nameof(CanApplyTransparency));
        }
    }

    public string TransparencyPercentText
    {
        get => _transparencyPercentText;
        set
        {
            if (_transparencyPercentText == value)
            {
                return;
            }

            _transparencyPercentText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanApplyTransparency));
        }
    }

    public bool TryGetTransparencyPercent(out double transparencyPercent)
    {
        if (!double.TryParse(
            TransparencyPercentText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out transparencyPercent))
        {
            return false;
        }

        transparencyPercent = Math.Clamp(transparencyPercent, 0.0, 100.0);
        return true;
    }

    public bool CanApplyTransparency => HasSelectedReference && TryGetTransparencyPercent(out _);

    public bool HasReferences => References.Count > 0;

    public bool HasSelectedReference => SelectedReference is not null;

    public bool CanOpenSelectedFolder => SelectedReference is not null && !string.IsNullOrWhiteSpace(SelectedReference.DirectoryPath);

    public int ReferenceCount => References.Count;

    public int MissingCount => References.Count(reference => reference.IsMissing);

    public string SummaryText
    {
        get
        {
            if (References.Count == 0)
            {
                return "The drawing has no external raster image references.";
            }

            string noun = References.Count == 1 ? "reference" : "references";
            return MissingCount == 0
                ? $"{References.Count} image {noun}. All linked files were found."
                : $"{References.Count} image {noun}. {MissingCount} missing.";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
