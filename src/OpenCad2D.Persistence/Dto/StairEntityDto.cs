namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable parametric stair entity.
/// </summary>
public sealed class StairEntityDto : EntityDto
{
    public StairEntityDto()
    {
        Type = EntityTypeNames.Stair;
    }

    public double InsertionX { get; set; }

    public double InsertionY { get; set; }

    public string ViewKind { get; set; } = "Plan";

    public double Width { get; set; }

    public int TreadCount { get; set; }

    public double TreadDepth { get; set; }

    public double RiserHeight { get; set; }

    public bool ShowStructure { get; set; }

    public double SlabThickness { get; set; } = 3.0;

    public string PlanArrowMode { get; set; } = "FirstToLast";

    public bool ShowPlanSectionMarker { get; set; }

    public double XAxisX { get; set; } = 1.0;

    public double XAxisY { get; set; }

    public double YAxisX { get; set; }

    public double YAxisY { get; set; } = 1.0;
}
