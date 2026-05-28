using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Library;

public sealed class LibraryWindowViewModel : INotifyPropertyChanged
{
    private LibraryCatalogCategory? _selectedCategory;
    private LibraryCatalogItem? _selectedItem;
    private string _searchText = string.Empty;
    private string _validationMessage = string.Empty;

    public LibraryWindowViewModel(LibraryCatalogScanResult catalog)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Categories = new ObservableCollection<LibraryCatalogCategory>(Catalog.Categories);
        VisibleItems = new ObservableCollection<LibraryCatalogItem>();

        SelectedCategory = Categories.FirstOrDefault();
    }

    public LibraryCatalogScanResult Catalog { get; }

    public ObservableCollection<LibraryCatalogCategory> Categories { get; }

    public ObservableCollection<LibraryCatalogItem> VisibleItems { get; }

    public LibraryCatalogCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (ReferenceEquals(_selectedCategory, value))
            {
                return;
            }

            _selectedCategory = value;
            OnPropertyChanged();
            RefreshVisibleItems();
        }
    }

    public LibraryCatalogItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value))
            {
                return;
            }

            _selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(CanInsertSelectedItem));
            ClearValidation();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            string normalized = value ?? string.Empty;

            if (_searchText == normalized)
            {
                return;
            }

            _searchText = normalized;
            OnPropertyChanged();
            RefreshVisibleItems();
        }
    }

    public bool HasItems => Catalog.Items.Count > 0;

    public bool HasVisibleItems => VisibleItems.Count > 0;

    public bool HasSelectedItem => SelectedItem is not null;

    public bool CanInsertSelectedItem => SelectedItem is not null;

    public bool HasWarnings => Catalog.Warnings.Count > 0;

    public string SummaryText
    {
        get
        {
            int itemCount = Catalog.Items.Count;

            return itemCount == 1
                ? "1 library item"
                : $"{itemCount} library items";
        }
    }

    public string WarningSummaryText
    {
        get
        {
            int warningCount = Catalog.Warnings.Count;

            return warningCount == 1
                ? "1 invalid item skipped"
                : $"{warningCount} invalid items skipped";
        }
    }

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

    public bool TryBuildResult(out LibraryWindowResult result)
    {
        result = new LibraryWindowResult(null!);

        if (SelectedItem is null)
        {
            ValidationMessage = "Select a library item before inserting it.";
            return false;
        }

        result = new LibraryWindowResult(SelectedItem);
        ClearValidation();
        return true;
    }

    private void RefreshVisibleItems()
    {
        LibraryCatalogItem? previousSelection = SelectedItem;
        VisibleItems.Clear();

        if (SelectedCategory is not null)
        {
            string filter = SearchText.Trim();

            foreach (LibraryCatalogItem item in SelectedCategory.Items
                .Where(item => MatchesFilter(item, filter)))
            {
                VisibleItems.Add(item);
            }
        }

        SelectedItem = previousSelection is not null && VisibleItems.Contains(previousSelection)
            ? previousSelection
            : VisibleItems.FirstOrDefault();

        OnPropertyChanged(nameof(HasVisibleItems));
    }

    private static bool MatchesFilter(
        LibraryCatalogItem item,
        string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return item.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            item.Category.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            item.RelativePath.Contains(filter, StringComparison.OrdinalIgnoreCase);
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
