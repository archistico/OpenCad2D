namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable dimension style definition.
/// </summary>
public sealed class DimensionStyleDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TextFormatId { get; set; } = "Annotation";

    public double ArrowSize { get; set; } = 4.0;

    public double TextOffset { get; set; } = 2.0;

    public double ExtensionLineOffset { get; set; } = 1.5;

    public double ExtensionLineOvershoot { get; set; } = 2.0;

    public int DecimalPlaces { get; set; } = 2;

    public string DecimalSeparator { get; set; } = ".";

    public string Suffix { get; set; } = string.Empty;
}
