using OpenCad2D.Export.Svg;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenCad2D.App.ViewModels.Svg;

/// <summary>
/// View-model used by the SVG export settings dialog.
/// </summary>
public sealed class SvgExportSettingsWindowViewModel
{
    public SvgExportSettingsWindowViewModel()
        : this(SvgExportOptions.Default)
    {
    }

    public SvgExportSettingsWindowViewModel(SvgExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        SelectedBackgroundMode = options.IncludeBackground
            ? options.BackgroundMode
            : SvgBackgroundMode.Transparent;
        MarginText = options.Margin.ToString("0.##", CultureInfo.InvariantCulture);
        IncludeHiddenLayers = options.IncludeHiddenLayers;
        GroupByLayer = options.GroupByLayer;
        IncludeMetadata = options.IncludeMetadata;
    }

    public IReadOnlyList<SvgBackgroundMode> BackgroundModes { get; } = new[]
    {
        SvgBackgroundMode.CanvasDark,
        SvgBackgroundMode.White,
        SvgBackgroundMode.Transparent
    };

    public SvgBackgroundMode SelectedBackgroundMode { get; set; } = SvgBackgroundMode.CanvasDark;

    public string MarginText { get; set; } = "20";

    public bool IncludeHiddenLayers { get; set; }

    public bool GroupByLayer { get; set; } = true;

    public bool IncludeMetadata { get; set; } = true;

    public SvgExportOptions CreateOptions(string title)
    {
        double margin = ParseMargin(MarginText);

        return new SvgExportOptions
        {
            Title = string.IsNullOrWhiteSpace(title)
                ? "OpenCad2D export"
                : title,
            Margin = margin,
            IncludeHiddenLayers = IncludeHiddenLayers,
            IncludeMetadata = IncludeMetadata,
            GroupByLayer = GroupByLayer,
            BackgroundMode = SelectedBackgroundMode,
            IncludeBackground = SelectedBackgroundMode != SvgBackgroundMode.Transparent
        };
    }

    private static double ParseMargin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SVG margin cannot be empty.");
        }

        string normalized = value.Trim();

        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out double margin) &&
            !double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out margin))
        {
            throw new ArgumentException("SVG margin must be a valid number.");
        }

        if (margin < 0)
        {
            throw new ArgumentException("SVG margin cannot be negative.");
        }

        if (margin > 500)
        {
            throw new ArgumentException("SVG margin cannot be greater than 500 drawing units.");
        }

        return margin;
    }
}
