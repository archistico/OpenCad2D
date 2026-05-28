using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.Library;

public sealed class LibraryCatalogScanResult
{
    public LibraryCatalogScanResult(
        IEnumerable<LibraryCatalogCategory> categories,
        IEnumerable<LibraryCatalogWarning> warnings)
    {
        Categories = (categories ?? throw new ArgumentNullException(nameof(categories)))
            .OrderBy(category => category.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Warnings = (warnings ?? throw new ArgumentNullException(nameof(warnings)))
            .OrderBy(warning => warning.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<LibraryCatalogCategory> Categories { get; }

    public IReadOnlyList<LibraryCatalogWarning> Warnings { get; }

    public IReadOnlyList<LibraryCatalogItem> Items => Categories
        .SelectMany(category => category.Items)
        .ToList();
}
