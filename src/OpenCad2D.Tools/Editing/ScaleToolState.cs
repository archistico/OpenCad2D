namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Represents the interaction state of <see cref="ScaleTool" />.
/// </summary>
public enum ScaleToolState
{
    WaitingForBasePoint,
    WaitingForReferencePoint,
    WaitingForDestinationPoint
}
