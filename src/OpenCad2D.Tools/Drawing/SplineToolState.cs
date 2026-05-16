namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive states used while drawing a Bezier spline.
/// </summary>
public enum SplineToolState
{
    WaitingForFirstPoint,
    CollectingControlPoints
}
