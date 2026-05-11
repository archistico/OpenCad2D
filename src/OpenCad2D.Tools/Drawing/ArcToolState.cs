namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Represents the current interactive step of the arc drawing tool.
/// </summary>
public enum ArcToolState
{
    /// <summary>The tool is waiting for the arc center point.</summary>
    WaitingForCenterPoint,

    /// <summary>The tool is waiting for the arc start point, which also defines the radius.</summary>
    WaitingForStartPoint,

    /// <summary>The tool is waiting for the arc end direction point.</summary>
    WaitingForEndPoint
}
