namespace OpenCad2D.Tools.Common;

/// <summary>
/// Describes the result of a tool interaction.
/// </summary>
public enum ToolResultKind
{
    None,
    Started,
    Updated,
    Completed,
    Cancelled
}