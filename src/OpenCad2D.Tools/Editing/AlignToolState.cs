namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Interaction state of the align tool.
/// </summary>
public enum AlignToolState
{
    WaitingForSourcePoint1,
    WaitingForDestinationPoint1,
    WaitingForSourcePoint2,
    WaitingForDestinationPoint2,
    WaitingForScaleConfirmation
}
