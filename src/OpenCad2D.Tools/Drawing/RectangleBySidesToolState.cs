namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Describes the current interactive phase of the rectangle-by-sides tool.
/// </summary>
public enum RectangleBySidesToolState
{
    WaitingForStartPoint,
    WaitingForFirstSideEndPoint,
    WaitingForSecondSidePoint
}
