namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable horizontal or vertical linear dimension entity.
/// </summary>
public sealed class LinearDimensionEntityDto : EntityDto
{
    public LinearDimensionEntityDto()
    {
        Type = EntityTypeNames.LinearDimension;
    }

    public double FirstX { get; set; }

    public double FirstY { get; set; }

    public double SecondX { get; set; }

    public double SecondY { get; set; }

    public double DimensionLineX { get; set; }

    public double DimensionLineY { get; set; }

    public string Orientation { get; set; } = "Horizontal";

    public string DimensionStyleId { get; set; } = "Standard";

    public string? TextOverride { get; set; }
}
