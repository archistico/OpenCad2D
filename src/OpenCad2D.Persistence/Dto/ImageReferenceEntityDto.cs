namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable external raster image reference entity.
/// </summary>
public sealed class ImageReferenceEntityDto : EntityDto
{
    public ImageReferenceEntityDto()
    {
        Type = EntityTypeNames.ImageReference;
    }

    public string FilePath { get; set; } = string.Empty;

    public double OriginX { get; set; }

    public double OriginY { get; set; }

    public double WidthVectorX { get; set; }

    public double WidthVectorY { get; set; }

    public double HeightVectorX { get; set; }

    public double HeightVectorY { get; set; }

    public int PixelWidth { get; set; }

    public int PixelHeight { get; set; }

    public double Opacity { get; set; } = 1.0;
}
