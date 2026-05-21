namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable document-level editor settings.
/// These settings are stored in the .opencad2d.json file so each drawing can
/// reopen with its own grid, snap and drafting defaults.
/// </summary>
public sealed class DocumentSettingsDto
{
    public string CurrentLayerId { get; set; } = "0";

    public string CurrentTextFormatId { get; set; } = "Standard";

    public string CurrentDimensionStyleId { get; set; } = "Standard";

    public DocumentGridSettingsDto Grid { get; set; } = new();

    public DocumentSnapSettingsDto Snapping { get; set; } = new();

    public DocumentDraftingSettingsDto Drafting { get; set; } = new();
}

public sealed class DocumentGridSettingsDto
{
    public string Kind { get; set; } = "Rectangular";

    public bool IsVisible { get; set; } = true;

    public double MinorStep { get; set; } = 10;

    public double MajorStep { get; set; } = 50;

    public double OriginX { get; set; }

    public double OriginY { get; set; }

    public double MinimumScreenSpacing { get; set; } = 8;

    public double MaximumScreenSpacing { get; set; } = 220;

    public double IsometricAngleDegrees { get; set; } = 30;
}

public sealed class DocumentSnapSettingsDto
{
    public bool IsEnabled { get; set; } = true;

    public List<string> EnabledModes { get; set; } = new()
    {
        "Endpoint",
        "Midpoint",
        "Center",
        "Quadrant",
        "Intersection",
        "Perpendicular",
        "Tangent",
        "Grid"
    };

    public double Tolerance { get; set; } = 8;
}

public sealed class DocumentDraftingSettingsDto
{
    public bool IsOrthoEnabled { get; set; }

    public DocumentPolarTrackingSettingsDto PolarTracking { get; set; } = new();
}

public sealed class DocumentPolarTrackingSettingsDto
{
    public bool IsEnabled { get; set; }

    public double StepDegrees { get; set; } = 90;
}
