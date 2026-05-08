using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Coordinates global CAD actions such as undo, redo, delete selection and cancel.
/// </summary>
public sealed class CadActionController
{
    private readonly ToolContext _context;
    private readonly ToolController _toolController;

    public CadActionController(
        ToolContext context,
        ToolController toolController)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(toolController);

        _context = context;
        _toolController = toolController;
    }

    public bool CanUndo => _context.CommandHistory.CanUndo;

    public bool CanRedo => _context.CommandHistory.CanRedo;

    public bool HasSelection => !_context.SelectionSet.IsEmpty;

    public ToolResult Undo()
    {
        if (!_context.CommandHistory.CanUndo)
        {
            return ToolResult.None("Nothing to undo.");
        }

        _context.CommandHistory.Undo(_context.Document);

        return ToolResult.Completed("Undo completed.");
    }

    public ToolResult Redo()
    {
        if (!_context.CommandHistory.CanRedo)
        {
            return ToolResult.None("Nothing to redo.");
        }

        _context.CommandHistory.Redo(_context.Document);

        return ToolResult.Completed("Redo completed.");
    }

    public ToolResult DeleteSelection()
    {
        var deleteTool = new DeleteTool();

        return deleteTool.Execute(_context);
    }

    public ToolResult CancelActiveTool()
    {
        return _toolController.CancelActiveTool();
    }
}