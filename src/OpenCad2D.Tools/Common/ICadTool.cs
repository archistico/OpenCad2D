namespace OpenCad2D.Tools.Common;

/// <summary>
/// Base interface for interactive CAD tools.
/// </summary>
public interface ICadTool
{
    string Name { get; }

    ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer);

    ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer);

    ToolResult OnPointerReleased(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return ToolResult.None();
    }

    /// <summary>
    /// Explicitly cancels the current tool operation.
    /// For example, Escape pressed by the user.
    /// </summary>
    ToolResult Cancel(ToolContext context);

    /// <summary>
    /// Deactivates the tool because another tool is becoming active.
    /// This should not necessarily have the same effect as Cancel.
    /// </summary>
    ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ToolResult.None();
    }
}