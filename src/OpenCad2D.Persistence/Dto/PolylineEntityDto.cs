namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable polyline entity.
/// </summary>
public sealed class PolylineEntityDto : EntityDto
{
    public PolylineEntityDto()
    {
        Type = EntityTypeNames.Polyline;
    }

    public bool IsClosed { get; set; }

    public bool IsFilled { get; set; }

    public List<PointDto> Vertices { get; set; } = new();

    public List<double>? SegmentBulges { get; set; }
}
