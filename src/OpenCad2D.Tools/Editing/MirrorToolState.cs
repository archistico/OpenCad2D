namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Workflow phases for the mirror tool.
/// </summary>
public enum MirrorToolState
{
    WaitingForEntitySelection,
    WaitingForFirstAxisPoint,
    WaitingForSecondAxisPoint,
    WaitingForDeleteSourceOption
}
