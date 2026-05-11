namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable reusable line format.
/// </summary>
public sealed class LineFormatDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#FFFFFF";

    public double LineWeight { get; set; } = 1.0;

    public string LineStyle { get; set; } = "Continuous";
}
