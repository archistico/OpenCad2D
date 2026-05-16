namespace OpenCad2D.Tools.Input;

/// <summary>
/// Describes the legacy low-level coordinate parser result kind.
/// The v0.8 command system uses <see cref="CommandInputKind" /> for prompt expectations
/// and <see cref="CommandInputSubmissionKind" /> for contextual submissions.
/// </summary>
public enum CommandInputParseKind
{
    Invalid = 0,
    AbsolutePoint = 1,
    RelativePoint = 2,
    Distance = 3,
    DistanceAngle = 4
}
