namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive state for the ellipse tool.
/// </summary>
public enum EllipseToolState
{
    WaitingForCenter,
    WaitingForMajorAxis,
    WaitingForMinorRadius
}
