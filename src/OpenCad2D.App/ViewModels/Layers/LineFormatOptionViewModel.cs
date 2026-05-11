using Avalonia.Media;
using System;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using System.Globalization;

namespace OpenCad2D.App.ViewModels.Layers;

public sealed class LineFormatOptionViewModel
{
    public LineFormatOptionViewModel(LineFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);

        Id = format.Id;
        Name = format.Name;
        LineWeightText = format.LineWeight.Millimeters.ToString("0.###", CultureInfo.InvariantCulture);
        LineStyleText = format.LineStyle.ToString();
        ColorBrush = new SolidColorBrush(Color.FromRgb(
            format.Color.R,
            format.Color.G,
            format.Color.B));
        DisplayText = $"{Name} — {LineWeightText} — {LineStyleText}";
    }

    public LineFormatId Id { get; }

    public string Name { get; }

    public string LineWeightText { get; }

    public string LineStyleText { get; }

    public IBrush ColorBrush { get; }

    public string DisplayText { get; }
}
