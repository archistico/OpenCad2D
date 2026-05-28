using System;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.ViewModels.Library;

public sealed class LibraryCatalogItem
{
    public LibraryCatalogItem(
        string id,
        string title,
        string category,
        string filePath,
        string relativePath,
        DocumentDto document)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Library item id cannot be empty.", nameof(id))
            : id;
        Title = string.IsNullOrWhiteSpace(title)
            ? throw new ArgumentException("Library item title cannot be empty.", nameof(title))
            : title;
        Category = string.IsNullOrWhiteSpace(category)
            ? throw new ArgumentException("Library item category cannot be empty.", nameof(category))
            : category;
        FilePath = string.IsNullOrWhiteSpace(filePath)
            ? throw new ArgumentException("Library item file path cannot be empty.", nameof(filePath))
            : filePath;
        RelativePath = string.IsNullOrWhiteSpace(relativePath)
            ? throw new ArgumentException("Library item relative path cannot be empty.", nameof(relativePath))
            : relativePath;
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public string Id { get; }

    public string Title { get; }

    public string Category { get; }

    public string FilePath { get; }

    public string RelativePath { get; }

    public DocumentDto Document { get; }
}
