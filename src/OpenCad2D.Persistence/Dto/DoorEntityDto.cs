namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable parametric door entity.
/// </summary>
public sealed class DoorEntityDto : EntityDto
{
    public DoorEntityDto()
    {
        Type = EntityTypeNames.Door;
    }

    public double InsertionX { get; set; }

    public double InsertionY { get; set; }

    public double Width { get; set; }

    public double WallThickness { get; set; }

    public double OpeningAngleDegrees { get; set; } = 90.0;

    public string SwingDirection { get; set; } = "Left";

    public string Anchor { get; set; } = "MiddleLeft";

    public bool MaskWallOpening { get; set; } = true;

    public double XAxisX { get; set; } = 1.0;

    public double XAxisY { get; set; }

    public double YAxisX { get; set; }

    public double YAxisY { get; set; } = 1.0;
}
