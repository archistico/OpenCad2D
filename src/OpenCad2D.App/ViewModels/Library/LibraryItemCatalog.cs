using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.ViewModels.Library;

public sealed class LibraryItemCatalog
{
    public const string LibraryItemSearchPattern = "*.opencad2d.json";
    public const string UncategorizedCategoryName = "uncategorized";

    private readonly IDocumentSerializer _documentSerializer;

    public LibraryItemCatalog(IDocumentSerializer documentSerializer)
    {
        _documentSerializer = documentSerializer ?? throw new ArgumentNullException(nameof(documentSerializer));
    }

    public LibraryCatalogScanResult Scan(string libraryRootPath)
    {
        if (string.IsNullOrWhiteSpace(libraryRootPath))
        {
            throw new ArgumentException("Library root path cannot be empty.", nameof(libraryRootPath));
        }

        string rootPath = Path.GetFullPath(libraryRootPath);

        if (!Directory.Exists(rootPath))
        {
            return new LibraryCatalogScanResult(
                Array.Empty<LibraryCatalogCategory>(),
                Array.Empty<LibraryCatalogWarning>());
        }

        var items = new List<LibraryCatalogItem>();
        var warnings = new List<LibraryCatalogWarning>();

        foreach (string filePath in Directory.EnumerateFiles(
            rootPath,
            LibraryItemSearchPattern,
            SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string fullFilePath = Path.GetFullPath(filePath);
            string relativePath = Path.GetRelativePath(rootPath, fullFilePath);
            string category = GetCategoryName(relativePath);

            try
            {
                DocumentDto document = _documentSerializer.LoadFromFile(fullFilePath);
                string title = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fullFilePath));
                string id = BuildItemId(category, title);

                items.Add(new LibraryCatalogItem(
                    id,
                    title,
                    category,
                    fullFilePath,
                    relativePath,
                    document));
            }
            catch (Exception exception) when (exception is ArgumentException or DocumentLoadException or UnsupportedDocumentVersionException)
            {
                warnings.Add(new LibraryCatalogWarning(
                    fullFilePath,
                    relativePath,
                    exception.Message));
            }
        }

        List<LibraryCatalogCategory> categories = items
            .GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LibraryCatalogCategory(
                group.Key,
                BuildDisplayName(group.Key),
                group))
            .ToList();

        return new LibraryCatalogScanResult(categories, warnings);
    }

    private static string GetCategoryName(string relativePath)
    {
        string[] parts = relativePath.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 1
            ? parts[0]
            : UncategorizedCategoryName;
    }

    private static string BuildDisplayName(string category)
    {
        return category == UncategorizedCategoryName
            ? "Uncategorized"
            : category;
    }

    private static string BuildItemId(
        string category,
        string title)
    {
        return $"{NormalizeIdPart(category)}.{NormalizeIdPart(title)}";
    }

    private static string NormalizeIdPart(string value)
    {
        string normalized = new(
            value
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
                .ToArray());

        normalized = string.Join(
            "-",
            normalized.Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(normalized)
            ? "item"
            : normalized;
    }
}
