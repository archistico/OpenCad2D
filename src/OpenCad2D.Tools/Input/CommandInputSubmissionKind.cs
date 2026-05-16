namespace OpenCad2D.Tools.Input;

/// <summary>
/// Describes the contextual value submitted by the user to the active CAD command.
/// </summary>
public enum CommandInputSubmissionKind
{
    Invalid = 0,
    Command = 1,
    Point = 2,
    Distance = 3,
    Angle = 4,
    Number = 5,
    Text = 6,
    Option = 7,
    Confirm = 8,
    Selection = 9
}
