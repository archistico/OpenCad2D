namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable CAD layer.
/// </summary>
public sealed class LayerDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the reusable line format assigned to this layer.
    /// </summary>
    public string LineFormatId { get; set; } = "Continuous";

    /// <summary>
    /// Legacy field kept only so older JSON files can still be read.
    /// New documents should use LineFormatId instead.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Legacy field kept only so older JSON files can still be read.
    /// New documents should use LineFormatId instead.
    /// </summary>
    public double? LineWeight { get; set; }

    /// <summary>
    /// Solid fill color used by fillable entities on this layer.
    /// Missing values are resolved from the layer line format color for backward compatibility.
    /// </summary>
    public string? FillColor { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; }
}
