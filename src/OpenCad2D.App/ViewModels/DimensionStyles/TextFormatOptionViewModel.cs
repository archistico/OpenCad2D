using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using System;

namespace OpenCad2D.App.ViewModels.DimensionStyles;

public sealed class TextFormatOptionViewModel
{
    public TextFormatOptionViewModel(TextFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);

        Id = format.Id;
        Name = format.Name;
        DisplayText = $"{format.Name} — {format.FontFamily} — {format.Height:0.###}";
    }

    public TextFormatId Id { get; }

    public string Name { get; }

    public string DisplayText { get; }
}
