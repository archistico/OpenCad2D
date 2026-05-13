using OpenCad2D.Export.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenCad2D.App.ViewModels.Pdf;

/// <summary>
/// View-model used by the PDF export settings dialog.
/// </summary>
public sealed class PdfExportSettingsWindowViewModel
{
    public PdfExportSettingsWindowViewModel()
        : this(PdfExportOptions.Default)
    {
    }

    public PdfExportSettingsWindowViewModel(PdfExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        SelectedPageSize = options.PageSize;
        SelectedOrientation = options.Orientation;
        MarginMillimetersText = options.MarginMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        IncludeHiddenLayers = options.IncludeHiddenLayers;
        UsePrintFriendlyColors = options.UsePrintFriendlyColors;
    }

    public IReadOnlyList<PdfPageSize> PageSizes { get; } = new[]
    {
        PdfPageSize.A4,
        PdfPageSize.A3,
        PdfPageSize.A2,
        PdfPageSize.A1,
        PdfPageSize.A0
    };

    public IReadOnlyList<PdfPageOrientation> Orientations { get; } = new[]
    {
        PdfPageOrientation.Portrait,
        PdfPageOrientation.Landscape
    };

    public PdfPageSize SelectedPageSize { get; set; }

    public PdfPageOrientation SelectedOrientation { get; set; }

    public string MarginMillimetersText { get; set; } = "10";

    public bool IncludeHiddenLayers { get; set; }

    public bool UsePrintFriendlyColors { get; set; } = true;

    public PdfExportOptions CreateOptions()
    {
        double marginMillimeters = ParseMargin(MarginMillimetersText);

        return new PdfExportOptions
        {
            PageSize = SelectedPageSize,
            Orientation = SelectedOrientation,
            MarginMillimeters = marginMillimeters,
            IncludeHiddenLayers = IncludeHiddenLayers,
            UsePrintFriendlyColors = UsePrintFriendlyColors
        };
    }

    private static double ParseMargin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("PDF margin cannot be empty.");
        }

        string normalized = value.Trim();

        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out double marginMillimeters) &&
            !double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out marginMillimeters))
        {
            throw new ArgumentException("PDF margin must be a valid number.");
        }

        if (marginMillimeters < 0)
        {
            throw new ArgumentException("PDF margin cannot be negative.");
        }

        if (marginMillimeters > 100)
        {
            throw new ArgumentException("PDF margin cannot be greater than 100 mm.");
        }

        return marginMillimeters;
    }
}
