using OpenCad2D.App.ViewModels.Library;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.Tests;

public sealed class LibraryItemCatalogTests
{
    [Fact]
    public void Scan_WhenRootDoesNotExist_ShouldReturnEmptyResult()
    {
        string rootPath = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-library-missing-{Guid.NewGuid():N}");
        var catalog = new LibraryItemCatalog(new JsonDocumentSerializer());

        LibraryCatalogScanResult result = catalog.Scan(rootPath);

        Assert.Empty(result.Categories);
        Assert.Empty(result.Items);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Scan_ShouldFindItemsAndGroupByFirstFolderBelowLibraryRoot()
    {
        string rootPath = CreateTempDirectory();

        try
        {
            SaveLibraryDocument(rootPath, "arredo", "chair.opencad2d.json");
            SaveLibraryDocument(rootPath, "simboli", "north-simple.opencad2d.json");
            File.WriteAllText(Path.Combine(rootPath, "arredo", "notes.txt"), "not a library item");

            var catalog = new LibraryItemCatalog(new JsonDocumentSerializer());

            LibraryCatalogScanResult result = catalog.Scan(rootPath);

            Assert.Empty(result.Warnings);
            Assert.Equal(2, result.Categories.Count);
            Assert.Contains(result.Categories, category => category.Name == "arredo");
            Assert.Contains(result.Categories, category => category.Name == "simboli");
            Assert.Contains(result.Items, item =>
                item.Category == "arredo" &&
                item.Title == "chair" &&
                item.Id == "arredo.chair");
            Assert.Contains(result.Items, item =>
                item.Category == "simboli" &&
                item.Title == "north-simple" &&
                item.Id == "simboli.north-simple");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_WhenItemIsInvalid_ShouldReportWarningAndContinue()
    {
        string rootPath = CreateTempDirectory();

        try
        {
            SaveLibraryDocument(rootPath, "arredo", "chair.opencad2d.json");
            string invalidPath = Path.Combine(rootPath, "simboli", "broken.opencad2d.json");
            Directory.CreateDirectory(Path.GetDirectoryName(invalidPath)!);
            File.WriteAllText(invalidPath, "{ not valid json");

            var catalog = new LibraryItemCatalog(new JsonDocumentSerializer());

            LibraryCatalogScanResult result = catalog.Scan(rootPath);

            LibraryCatalogItem item = Assert.Single(result.Items);
            LibraryCatalogWarning warning = Assert.Single(result.Warnings);

            Assert.Equal("chair", item.Title);
            Assert.EndsWith("broken.opencad2d.json", warning.RelativePath);
            Assert.Contains("Cannot load OpenCad2D document", warning.Message);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_WhenItemIsNested_ShouldUseTopLevelFolderAsCategory()
    {
        string rootPath = CreateTempDirectory();

        try
        {
            SaveLibraryDocument(rootPath, Path.Combine("arredo", "sedie"), "chair.opencad2d.json");

            var catalog = new LibraryItemCatalog(new JsonDocumentSerializer());

            LibraryCatalogScanResult result = catalog.Scan(rootPath);

            LibraryCatalogItem item = Assert.Single(result.Items);

            Assert.Equal("arredo", item.Category);
            Assert.Equal(Path.Combine("arredo", "sedie", "chair.opencad2d.json"), item.RelativePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ShouldLoadValidDocumentForPreviewOrInsertionConsumers()
    {
        string rootPath = CreateTempDirectory();

        try
        {
            SaveLibraryDocument(rootPath, "arredo", "table.opencad2d.json");

            var catalog = new LibraryItemCatalog(new JsonDocumentSerializer());

            LibraryCatalogScanResult result = catalog.Scan(rootPath);

            LibraryCatalogItem item = Assert.Single(result.Items);

            Assert.Single(item.Document.Entities);
            Assert.Equal(JsonDocumentSerializer.CurrentVersion, item.Document.Version);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-library-{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);

        return path;
    }

    private static void SaveLibraryDocument(
        string rootPath,
        string relativeDirectory,
        string fileName)
    {
        string directory = Path.Combine(rootPath, relativeDirectory);
        Directory.CreateDirectory(directory);

        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            Point2D.Origin,
            new Point2D(10, 0)));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        serializer.SaveToFile(
            dto,
            Path.Combine(directory, fileName));
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
