using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.Properties;

public sealed class PropertySectionViewModel
{
    public PropertySectionViewModel(
        string title,
        IEnumerable<PropertyRowViewModel> rows)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Section title cannot be empty.",
                nameof(title));
        }

        ArgumentNullException.ThrowIfNull(rows);

        Title = title;
        Rows = rows.ToList();
    }

    public string Title { get; }

    public IReadOnlyList<PropertyRowViewModel> Rows { get; }
}
