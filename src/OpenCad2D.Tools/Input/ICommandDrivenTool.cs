using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Implemented by tools that expose a CAD-style prompt and can consume contextual command input.
/// </summary>
public interface ICommandDrivenTool
{
    /// <summary>
    /// True after terminal confirmation; completing an individual edit need not finish the command.
    /// </summary>
    bool IsCommandFinished => false;

    CommandPromptState GetPromptState(ToolContext context);

    ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context);
}
