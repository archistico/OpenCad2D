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

    ToolResult Cancel(ToolContext context);
}