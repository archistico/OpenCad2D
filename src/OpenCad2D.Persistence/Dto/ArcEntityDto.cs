namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable arc entity.
/// </summary>
public sealed class ArcEntityDto : EntityDto
{
    public ArcEntityDto()
    {
        Type = EntityTypeNames.Arc;
    }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double Radius { get; set; }

    public double StartAngleDegrees { get; set; }

    public double EndAngleDegrees { get; set; }

    public bool IsCounterClockwise { get; set; } = true;
}
