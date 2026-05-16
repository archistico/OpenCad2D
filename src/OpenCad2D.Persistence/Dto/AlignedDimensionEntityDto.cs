namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable aligned dimension entity.
/// </summary>
public sealed class AlignedDimensionEntityDto : EntityDto
{
    public AlignedDimensionEntityDto()
    {
        Type = EntityTypeNames.AlignedDimension;
    }

    public double FirstX { get; set; }

    public double FirstY { get; set; }

    public double SecondX { get; set; }

    public double SecondY { get; set; }

    public double DimensionLineX { get; set; }

    public double DimensionLineY { get; set; }

    public string DimensionStyleId { get; set; } = "Standard";

    public string? TextOverride { get; set; }

    public bool IsStale { get; set; }
}
