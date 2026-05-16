namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Tracks the interactive phases of the polygon drawing tool.
/// </summary>
public enum PolygonToolState
{
    WaitingForSides,
    WaitingForCenter,
    WaitingForVertex
}
