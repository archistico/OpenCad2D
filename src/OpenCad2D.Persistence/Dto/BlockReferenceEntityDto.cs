namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable block reference entity.
/// </summary>
public sealed class BlockReferenceEntityDto : EntityDto
{
    public BlockReferenceEntityDto()
    {
        Type = EntityTypeNames.BlockReference;
    }

    public string BlockDefinitionId { get; set; } = string.Empty;

    public double InsertionX { get; set; }

    public double InsertionY { get; set; }

    public double XAxisX { get; set; } = 1;

    public double XAxisY { get; set; }

    public double YAxisX { get; set; }

    public double YAxisY { get; set; } = 1;

    public double DefinitionMinX { get; set; }

    public double DefinitionMinY { get; set; }

    public double DefinitionMaxX { get; set; }

    public double DefinitionMaxY { get; set; }
}
