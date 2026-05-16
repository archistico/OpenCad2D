namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable angular dimension entity.
/// </summary>
public sealed class AngularDimensionEntityDto : EntityDto
{
    public AngularDimensionEntityDto()
    {
        Type = EntityTypeNames.AngularDimension;
    }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double FirstRayX { get; set; }

    public double FirstRayY { get; set; }

    public double SecondRayX { get; set; }

    public double SecondRayY { get; set; }

    public double ArcX { get; set; }

    public double ArcY { get; set; }

    public bool IsCounterClockwise { get; set; }

    public string DimensionStyleId { get; set; } = "Standard";

    public string? TextOverride { get; set; }

    public bool IsStale { get; set; }
}
