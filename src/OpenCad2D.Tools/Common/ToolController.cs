namespace OpenCad2D.Tools.Common;

/// <summary>
/// Coordinates the active CAD tool and forwards input events to it.
/// </summary>
public sealed class ToolController
{
    private readonly ToolContext _context;

    public ToolController(
        ToolContext context,
        ICadTool initialTool)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(initialTool);

        _context = context;
        ActiveTool = initialTool;
        LastResult = ToolResult.None();
    }

    public ICadTool ActiveTool { get; private set; }

    public string ActiveToolName => ActiveTool.Name;

    public ToolResult LastResult { get; private set; }

    public ToolResult SetActiveTool(
        ICadTool tool,
        bool cancelCurrentTool = true)
    {
        ArgumentNullException.ThrowIfNull(tool);

        ToolResult result = ToolResult.None();

        if (cancelCurrentTool)
        {
            result = ActiveTool.Cancel(_context);
        }

        ActiveTool = tool;
        LastResult = result;

        return result;
    }

    public ToolResult OnPointerPressed(PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);

        LastResult = ActiveTool.OnPointerPressed(
            _context,
            pointer);

        return LastResult;
    }

    public ToolResult OnPointerMoved(PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);

        LastResult = ActiveTool.OnPointerMoved(
            _context,
            pointer);

        return LastResult;
    }

    public ToolResult OnPointerReleased(PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);

        LastResult = ActiveTool.OnPointerReleased(
            _context,
            pointer);

        return LastResult;
    }

    public ToolResult CancelActiveTool()
    {
        LastResult = ActiveTool.Cancel(_context);

        return LastResult;
    }
}