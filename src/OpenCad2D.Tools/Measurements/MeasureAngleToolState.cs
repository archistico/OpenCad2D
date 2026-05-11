namespace OpenCad2D.Tools.Measurements;

/// <summary>
/// States of the three-point angle measurement tool.
/// </summary>
public enum MeasureAngleToolState
{
    WaitingForFirstRayPoint,
    WaitingForVertex,
    WaitingForSecondRayPoint
}
