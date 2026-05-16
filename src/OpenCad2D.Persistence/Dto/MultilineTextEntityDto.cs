namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable multiline text entity.
/// </summary>
public sealed class MultilineTextEntityDto : EntityDto
{
    public MultilineTextEntityDto()
    {
        Type = EntityTypeNames.MultilineText;
    }

    public string Text { get; set; } = string.Empty;

    public double InsertionX { get; set; }

    public double InsertionY { get; set; }

    public double RotationDegrees { get; set; }

    public string TextFormatId { get; set; } = "Standard";
}
