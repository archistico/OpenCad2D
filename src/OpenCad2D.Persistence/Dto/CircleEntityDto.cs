namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable circle entity.
/// </summary>
public sealed class CircleEntityDto : EntityDto
{
    public CircleEntityDto()
    {
        Type = EntityTypeNames.Circle;
    }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double Radius { get; set; }
}
