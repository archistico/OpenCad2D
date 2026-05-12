namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable reusable single-line text format.
/// </summary>
public sealed class TextFormatDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Arial";

    public double Height { get; set; } = 2.5;

    public string Color { get; set; } = "#FFFFFF";

    public bool IsBold { get; set; }

    public bool IsItalic { get; set; }
}
