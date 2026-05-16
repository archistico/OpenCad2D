namespace OpenCad2D.Tools.Input;

/// <summary>
/// Describes what the active command prompt is currently expecting from the command input.
/// </summary>
public enum CommandInputKind
{
    None = 0,
    CommandName = 1,
    Point = 2,
    Distance = 3,
    Angle = 4,
    Number = 5,
    Text = 6,
    Selection = 7,
    Option = 8,
    PointOrOption = 9,
    DistanceOrOption = 10,
    SelectionOrOption = 11,
    PointOrDistance = 12,
    PointOrDistanceOrOption = 13
}
