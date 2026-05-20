namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable native elliptical arc entity.
/// </summary>
public sealed class EllipticalArcEntityDto : EntityDto
{
    public EllipticalArcEntityDto()
    {
        Type = EntityTypeNames.EllipticalArc;
    }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double MajorAxisX { get; set; }

    public double MajorAxisY { get; set; }

    public double MinorRadius { get; set; }

    public double StartParameterRadians { get; set; }

    public double EndParameterRadians { get; set; }

    public bool IsCounterClockwise { get; set; } = true;
}
