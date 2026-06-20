namespace OpenCad2D.Core.Entities;

/// <summary>
/// Identifies the concrete kind of a CAD entity.
/// </summary>
public enum EntityKind
{
    Point,
    Text,
    MultilineText,
    HorizontalDimension,
    VerticalDimension,
    AlignedDimension,
    RadiusDimension,
    DiameterDimension,
    AngularDimension,
    Line,
    Circle,
    Ellipse,
    EllipticalArc,
    Arc,
    Polyline,
    BezierSpline,
    ImageReference,
    BlockReference,
    Stair
}