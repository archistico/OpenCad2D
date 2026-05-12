namespace OpenCad2D.Core.Entities;

/// <summary>
/// Identifies the concrete kind of a CAD entity.
/// </summary>
public enum EntityKind
{
    Point,
    Text,
    HorizontalDimension,
    VerticalDimension,
    AlignedDimension,
    RadiusDimension,
    DiameterDimension,
    AngularDimension,
    Line,
    Circle,
    Arc,
    Polyline
}