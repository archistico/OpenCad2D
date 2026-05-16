namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable radius dimension entity.
/// </summary>
public sealed class RadiusDimensionEntityDto : EntityDto
{
    public RadiusDimensionEntityDto()
    {
        Type = EntityTypeNames.RadiusDimension;
    }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double PointOnCircleX { get; set; }

    public double PointOnCircleY { get; set; }

    public double TextX { get; set; }

    public double TextY { get; set; }

    public string DimensionStyleId { get; set; } = "Standard";

    public string? TextOverride { get; set; }

    public bool IsStale { get; set; }
}
