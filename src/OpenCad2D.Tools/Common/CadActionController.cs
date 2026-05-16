using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
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
        EnsureCurrentLayerIsUsable();

        return ToolResult.Completed("Undo completed.");
    }

    public ToolResult Redo()
    {
        if (!_context.CommandHistory.CanRedo)
        {
            return ToolResult.None("Nothing to redo.");
        }

        _context.CommandHistory.Redo(_context.Document);
        EnsureCurrentLayerIsUsable();

        return ToolResult.Completed("Redo completed.");
    }

    public ToolResult DeleteSelection()
    {
        var deleteTool = new DeleteTool();

        return deleteTool.Execute(_context);
    }

    public ToolResult SelectAll()
    {
        var selectableIds = _context.Document.GetSelectableEntities()
            .Select(entity => entity.Id)
            .ToList();

        if (selectableIds.Count == 0)
        {
            _context.SelectionSet.Clear();
            return ToolResult.None("No selectable entities found.");
        }

        _context.SelectionSet.ReplaceWith(selectableIds);

        return ToolResult.Completed($"Selected {selectableIds.Count} entities.");
    }

    public ToolResult SelectLast()
    {
        List<EntityId> previousSelectionIds = _context.SelectionSet.LastDeselectedSelectionIds
            .Where(id =>
            {
                return _context.Document.Entities.TryGet(id, out CadEntity? entity) &&
                       entity is not null &&
                       _context.Document.IsEntitySelectable(entity);
            })
            .ToList();

        if (previousSelectionIds.Count == 0)
        {
            return ToolResult.None("No previous selectable selection found.");
        }

        _context.SelectionSet.ReplaceWith(previousSelectionIds);

        return ToolResult.Completed(previousSelectionIds.Count == 1
            ? "Restored previous selection: 1 entity."
            : $"Restored previous selection: {previousSelectionIds.Count} entities.");
    }

    private void EnsureCurrentLayerIsUsable()
    {
        if (!_context.Document.Layers.Contains(_context.CurrentLayerId))
        {
            _context.CurrentLayerId = LayerId.Default;
        }

        Layer currentLayer = _context.Document.Layers.GetRequired(_context.CurrentLayerId);

        if (currentLayer.IsVisible && !currentLayer.IsLocked)
        {
            return;
        }

        Layer? firstUsableLayer = _context.Document.Layers.All
            .OrderBy(layer => layer.Name)
            .FirstOrDefault(layer => layer.IsVisible && !layer.IsLocked);

        _context.CurrentLayerId = firstUsableLayer?.Id ?? _context.CurrentLayerId;
    }

    public ToolResult CancelActiveTool()
    {
        return _toolController.CancelActiveTool();
    }
}