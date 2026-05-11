namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Represents the current interactive step of the three-point arc drawing tool.
/// </summary>
public enum ArcThreePointsToolState
{
    /// <summary>The tool is waiting for the arc start point.</summary>
    WaitingForStartPoint,

    /// <summary>The tool is waiting for a point that the arc must pass through.</summary>
    WaitingForPointOnArc,

    /// <summary>The tool is waiting for the arc end point.</summary>
    WaitingForEndPoint
}
