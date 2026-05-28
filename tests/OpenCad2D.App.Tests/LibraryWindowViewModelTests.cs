using OpenCad2D.App.ViewModels.Library;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.Tests;

public sealed class LibraryWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldSelectFirstCategoryAndFirstItem()
    {
        LibraryCatalogScanResult catalog = CreateCatalog(
            CreateItem("arredo", "chair"),
            CreateItem("simboli", "north"));

        var viewModel = new LibraryWindowViewModel(catalog);

        Assert.Equal("arredo", viewModel.SelectedCategory?.Name);
        Assert.Equal("chair", viewModel.SelectedItem?.Title);
        Assert.Equal(2, viewModel.Categories.Count);
        Assert.Single(viewModel.VisibleItems);
    }

    [Fact]
    public void SearchText_ShouldFilterVisibleItemsWithinSelectedCategory()
    {
        LibraryCatalogScanResult catalog = CreateCatalog(
            CreateItem("arredo", "chair"),
            CreateItem("arredo", "table"),
            CreateItem("simboli", "chair-symbol"));
        var viewModel = new LibraryWindowViewModel(catalog);

        viewModel.SearchText = "tab";

        LibraryCatalogItem item = Assert.Single(viewModel.VisibleItems);
        Assert.Equal("table", item.Title);
        Assert.Equal("table", viewModel.SelectedItem?.Title);
    }

    [Fact]
    public void TryBuildResult_WhenItemIsSelected_ShouldReturnSelectedItem()
    {
        LibraryCatalogItem item = CreateItem("arredo", "chair");
        var viewModel = new LibraryWindowViewModel(CreateCatalog(item));

        bool valid = viewModel.TryBuildResult(out LibraryWindowResult result);

        Assert.True(valid);
        Assert.Same(item, result.SelectedItem);
        Assert.False(viewModel.HasValidationMessage);
    }

    [Fact]
    public void TryBuildResult_WhenNoItemIsSelected_ShouldReject()
    {
        var catalog = new LibraryCatalogScanResult(
            Array.Empty<LibraryCatalogCategory>(),
            Array.Empty<LibraryCatalogWarning>());
        var viewModel = new LibraryWindowViewModel(catalog);

        bool valid = viewModel.TryBuildResult(out _);

        Assert.False(valid);
        Assert.True(viewModel.HasValidationMessage);
    }

    private static LibraryCatalogScanResult CreateCatalog(params LibraryCatalogItem[] items)
    {
        LibraryCatalogCategory[] categories = items
            .GroupBy(item => item.Category)
            .Select(group => new LibraryCatalogCategory(
                group.Key,
                group.Key,
                group))
            .ToArray();

        return new LibraryCatalogScanResult(
            categories,
            Array.Empty<LibraryCatalogWarning>());
    }

    private static LibraryCatalogItem CreateItem(
        string category,
        string title)
    {
        return new LibraryCatalogItem(
            $"{category}.{title}",
            title,
            category,
            Path.Combine("library", category, $"{title}.opencad2d.json"),
            Path.Combine(category, $"{title}.opencad2d.json"),
            new DocumentDto
            {
                Version = 1
            });
    }
}
