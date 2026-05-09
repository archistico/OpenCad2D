namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable line entity.
/// </summary>
public sealed class LineEntityDto : EntityDto
{
    public LineEntityDto()
    {
        Type = EntityTypeNames.Line;
    }

    public double StartX { get; set; }

    public double StartY { get; set; }

    public double EndX { get; set; }

    public double EndY { get; set; }
}
