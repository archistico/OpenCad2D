namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Entity type discriminator values used by the v1 JSON format.
/// </summary>
public static class EntityTypeNames
{
    public const string Point = "Point";

    public const string Text = "Text";
    public const string LinearDimension = "LinearDimension";
    public const string AlignedDimension = "AlignedDimension";
    public const string RadiusDimension = "RadiusDimension";
    public const string DiameterDimension = "DiameterDimension";
    public const string Line = "Line";
    public const string Circle = "Circle";
    public const string Arc = "Arc";
    public const string Polyline = "Polyline";
}
