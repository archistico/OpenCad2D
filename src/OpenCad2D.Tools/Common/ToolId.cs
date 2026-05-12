namespace OpenCad2D.Tools.Common;

/// <summary>
/// Identifies a CAD tool available in the application.
/// </summary>
public enum ToolId
{
    Selection,
    Point,
    Text,
    Line,
    Rectangle,
    RectangleBySides,
    Circle,
    Arc,
    ArcThreePoints,
    Polyline,
    HorizontalDimension,
    VerticalDimension,
    AlignedDimension,
    RadiusDimension,
    DiameterDimension,
    AngularDimension,
    Move,
    Copy,
    Rotate,
    Scale,
    Align,
    BreakAtPoint,
    BreakBetweenPoints,
    Extend,
    Trim,
    Delete,
    MeasureDistance,
    MeasureEntity,
    MeasureAngle,
    MeasureArea
}