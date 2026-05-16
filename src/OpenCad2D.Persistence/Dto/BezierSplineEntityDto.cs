namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable Bezier spline entity.
/// </summary>
public sealed class BezierSplineEntityDto : EntityDto
{
    public BezierSplineEntityDto()
    {
        Type = EntityTypeNames.BezierSpline;
    }

    public bool IsClosed { get; set; }

    public List<PointDto> ControlPoints { get; set; } = new();
}
