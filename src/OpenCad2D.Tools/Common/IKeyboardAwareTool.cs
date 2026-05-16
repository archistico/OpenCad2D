namespace OpenCad2D.Tools.Common;

/// <summary>
/// Optional interface for tools that need to handle keyboard shortcuts while active.
/// Keeps UI controls from checking concrete tool types for tool-specific key handling.
/// </summary>
public interface IKeyboardAwareTool
{
    bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result);
}
