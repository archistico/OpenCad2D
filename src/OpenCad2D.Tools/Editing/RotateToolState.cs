namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Represents the current input step of the rotate tool.
/// </summary>
public enum RotateToolState
{
    WaitingForEntitySelection,
    WaitingForBasePoint,
    WaitingForReferencePoint,
    WaitingForDestinationPoint
}
