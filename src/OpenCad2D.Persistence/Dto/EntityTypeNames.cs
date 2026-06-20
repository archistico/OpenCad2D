namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Entity type discriminator values used by the v1 JSON format.
/// </summary>
public static class EntityTypeNames
{
    public const string Point = "Point";

    public const string Text = "Text";
    public const string MultilineText = "MultilineText";
    public const string LinearDimension = "LinearDimension";
    public const string AlignedDimension = "AlignedDimension";
    public const string RadiusDimension = "RadiusDimension";
    public const string DiameterDimension = "DiameterDimension";
    public const string AngularDimension = "AngularDimension";
    public const string Line = "Line";
    public const string Circle = "Circle";
    public const string Ellipse = "Ellipse";
    public const string EllipticalArc = "EllipticalArc";
    public const string Arc = "Arc";
    public const string Polyline = "Polyline";
    public const string BezierSpline = "BezierSpline";

    public const string ImageReference = "ImageReference";

    public const string BlockReference = "BlockReference";

    public const string Stair = "Stair";
}
