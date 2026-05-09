namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable CAD layer.
/// </summary>
public sealed class LayerDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#FFFFFF";

    public double LineWeight { get; set; } = 0.25;

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; }
}
