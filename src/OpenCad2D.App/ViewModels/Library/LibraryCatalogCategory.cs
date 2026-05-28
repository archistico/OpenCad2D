using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.Library;

public sealed class LibraryCatalogCategory
{
    public LibraryCatalogCategory(
        string name,
        string displayName,
        IEnumerable<LibraryCatalogItem> items)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Library category name cannot be empty.", nameof(name))
            : name;
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? Name
            : displayName;
        Items = (items ?? throw new ArgumentNullException(nameof(items)))
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string Name { get; }

    public string DisplayName { get; }

    public IReadOnlyList<LibraryCatalogItem> Items { get; }
}
