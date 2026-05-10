using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.Properties;

public sealed class PropertyPanelViewModel
{
    public PropertyPanelViewModel(
        string title,
        IEnumerable<PropertySectionViewModel> sections)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Panel title cannot be empty.",
                nameof(title));
        }

        ArgumentNullException.ThrowIfNull(sections);

        Title = title;
        Sections = sections.ToList();
    }

    public string Title { get; }

    public IReadOnlyList<PropertySectionViewModel> Sections { get; }
}
