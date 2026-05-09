namespace OpenCad2D.Tools.Common;

/// <summary>
/// Result returned by an interactive CAD tool.
/// </summary>
public sealed class ToolResult
{
    private ToolResult(ToolResultKind kind, string? message = null)
    {
        Kind = kind;
        Message = message;
    }

    public ToolResultKind Kind { get; }

    public string? Message { get; }

    public static ToolResult None(string? message = null)
    {
        return new ToolResult(ToolResultKind.None, message);
    }

    public static ToolResult Started(string? message = null)
    {
        return new ToolResult(ToolResultKind.Started, message);
    }

    public static ToolResult Updated(string? message = null)
    {
        return new ToolResult(ToolResultKind.Updated, message);
    }

    public static ToolResult Completed(string? message = null)
    {
        return new ToolResult(ToolResultKind.Completed, message);
    }

    public static ToolResult Cancelled(string? message = null)
    {
        return new ToolResult(ToolResultKind.Cancelled, message);
    }

    public bool Changed =>
        Kind != ToolResultKind.None;
}