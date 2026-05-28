namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Represents the current input state of the polyline drawing tool.
/// </summary>
public enum PolylineToolState
{
    WaitingForFirstPoint,
    CollectingVertices,
    WaitingForArcPointOnArc,
    WaitingForArcEndPoint
}
