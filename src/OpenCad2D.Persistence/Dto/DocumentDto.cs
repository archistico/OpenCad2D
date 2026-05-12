namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable representation of an OpenCad2D document.
/// </summary>
public sealed class DocumentDto
{
    public int Version { get; set; }

    public string SavedAt { get; set; } = string.Empty;

    public DocumentSettingsDto Settings { get; set; } = new();

    public ViewportStateDto Viewport { get; set; } = new();

    public List<LineFormatDto> LineFormats { get; set; } = new();

    public List<TextFormatDto> TextFormats { get; set; } = new();

    public List<DimensionStyleDto> DimensionStyles { get; set; } = new();

    public List<LayerDto> Layers { get; set; } = new();

    public List<EntityDto> Entities { get; set; } = new();
}
