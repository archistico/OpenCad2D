namespace OpenCad2D.Tools.Input;

/// <summary>
/// Describes the kind of command-line point input entered by the user.
/// </summary>
public enum CommandInputKind
{
    Invalid = 0,
    AbsolutePoint = 1,
    RelativePoint = 2,
    Distance = 3
}
