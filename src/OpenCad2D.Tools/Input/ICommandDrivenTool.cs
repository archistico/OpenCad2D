using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Implemented by tools that expose a CAD-style prompt and can consume contextual command input.
/// </summary>
public interface ICommandDrivenTool
{
    CommandPromptState GetPromptState(ToolContext context);

    ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context);
}
