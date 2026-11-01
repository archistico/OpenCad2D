namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Represents the interaction state of <see cref="RotateTool" />.
/// </summary>
public enum RotateToolState
{
    WaitingForEntitySelection,
    WaitingForBasePoint,
    WaitingForRotationAngle,
    WaitingForReferenceAngle,
    WaitingForReferenceSecondPoint,
    WaitingForNewAngle,
    WaitingForNewAngleFirstPoint,
    WaitingForNewAngleSecondPoint
}
