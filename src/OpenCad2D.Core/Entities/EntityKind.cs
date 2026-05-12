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
    Line,
    Circle,
    Arc,
    Polyline
}