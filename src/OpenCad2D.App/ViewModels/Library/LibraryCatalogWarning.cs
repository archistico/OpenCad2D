using System;

namespace OpenCad2D.App.ViewModels.Library;

public sealed class LibraryCatalogWarning
{
    public LibraryCatalogWarning(
        string filePath,
        string relativePath,
        string message)
    {
        FilePath = string.IsNullOrWhiteSpace(filePath)
            ? throw new ArgumentException("Warning file path cannot be empty.", nameof(filePath))
            : filePath;
        RelativePath = string.IsNullOrWhiteSpace(relativePath)
            ? throw new ArgumentException("Warning relative path cannot be empty.", nameof(relativePath))
            : relativePath;
        Message = string.IsNullOrWhiteSpace(message)
            ? "Library item could not be loaded."
            : message;
    }

    public string FilePath { get; }

    public string RelativePath { get; }

    public string Message { get; }
}
