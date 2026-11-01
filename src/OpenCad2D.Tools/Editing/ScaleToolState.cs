namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Represents the interaction state of <see cref="ScaleTool" />.
/// </summary>
public enum ScaleToolState
{
    WaitingForEntitySelection,
    WaitingForBasePoint,
    WaitingForScaleFactor,
    WaitingForReferenceLength,
    WaitingForReferenceSecondPoint,
    WaitingForNewLength,
    WaitingForNewLengthFirstPoint,
    WaitingForNewLengthSecondPoint
}
