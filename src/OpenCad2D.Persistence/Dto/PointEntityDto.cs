namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable point entity.
/// </summary>
public sealed class PointEntityDto : EntityDto
{
    public PointEntityDto()
    {
        Type = EntityTypeNames.Point;
    }

    public double X { get; set; }

    public double Y { get; set; }
}
