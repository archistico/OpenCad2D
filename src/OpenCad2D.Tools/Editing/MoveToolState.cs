namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Describes the current phase of the interactive move command.
/// </summary>
public enum MoveToolState
{
    WaitingForEntitySelection,
    WaitingForBasePoint,
    WaitingForDestinationPoint
}
