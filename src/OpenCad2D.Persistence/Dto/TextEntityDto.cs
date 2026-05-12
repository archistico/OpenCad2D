namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable single-line text entity.
/// </summary>
public sealed class TextEntityDto : EntityDto
{
    public TextEntityDto()
    {
        Type = EntityTypeNames.Text;
    }

    public string Text { get; set; } = string.Empty;

    public double InsertionX { get; set; }

    public double InsertionY { get; set; }

    public double RotationDegrees { get; set; }

    public string TextFormatId { get; set; } = "Standard";
}
