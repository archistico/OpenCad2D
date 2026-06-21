namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable parametric window entity.
/// </summary>
public sealed class WindowEntityDto : EntityDto
{
    public WindowEntityDto()
    {
        Type = EntityTypeNames.Window;
    }

    public double InsertionX { get; set; }

    public double InsertionY { get; set; }

    public double Width { get; set; }

    public double WallThickness { get; set; }

    public double FrameOffset { get; set; } = 4.0;

    public string Anchor { get; set; } = "MiddleLeft";

    public bool MaskWallOpening { get; set; } = true;

    public double XAxisX { get; set; } = 1.0;

    public double XAxisY { get; set; }

    public double YAxisX { get; set; }

    public double YAxisY { get; set; } = 1.0;
}
