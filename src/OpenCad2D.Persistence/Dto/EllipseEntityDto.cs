namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable ellipse entity.
/// </summary>
public sealed class EllipseEntityDto : EntityDto
{
    public EllipseEntityDto()
    {
        Type = EntityTypeNames.Ellipse;
    }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double MajorAxisX { get; set; }

    public double MajorAxisY { get; set; }

    public double MinorRadius { get; set; }
}
