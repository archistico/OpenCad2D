using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Core.Commands;

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

    public ToolResult DeselectAll()
    {
        if (_context.SelectionSet.IsEmpty)
        {
            return ToolResult.None("No selected entities to deselect.");
        }

        int deselectedCount = _context.SelectionSet.Count;

        _context.SelectionSet.Clear();

        return ToolResult.Completed(deselectedCount == 1
            ? "Deselected 1 entity."
            : $"Deselected {deselectedCount} entities.");
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



    public ToolResult BringSelectionToFront()
    {
        return ApplyDrawOrder(DrawOrderOperation.BringToFront, "Brought selected entities to front.");
    }

    public ToolResult SendSelectionToBack()
    {
        return ApplyDrawOrder(DrawOrderOperation.SendToBack, "Sent selected entities to back.");
    }

    public ToolResult BringSelectionForward()
    {
        return ApplyDrawOrder(DrawOrderOperation.BringForward, "Moved selected entities forward.");
    }

    public ToolResult SendSelectionBackward()
    {
        return ApplyDrawOrder(DrawOrderOperation.SendBackward, "Moved selected entities backward.");
    }

    private ToolResult ApplyDrawOrder(
        DrawOrderOperation operation,
        string successMessage)
    {
        if (_context.SelectionSet.IsEmpty)
        {
            return ToolResult.None("No selected entities to reorder.");
        }

        List<EntityId> selectedIds = _context.SelectionSet.SelectedIds.ToList();

        var service = new DrawOrderService();
        IReadOnlyList<CadEntity> replacements = service.CreateReorderedEntities(
            _context.Document,
            selectedIds,
            operation);

        if (replacements.Count == 0)
        {
            return ToolResult.None("Selected entities cannot be reordered.");
        }

        _context.CommandHistory.Execute(
            _context.Document,
            new ReplaceEntitiesCommand(replacements));

        _context.SelectionSet.ReplaceWith(
            selectedIds.Where(id =>
                _context.Document.Entities.TryGet(id, out CadEntity? entity) &&
                entity is not null &&
                _context.Document.IsEntitySelectable(entity)));

        return ToolResult.Completed(successMessage);
    }


    public ToolResult AlignSelectionLeft()
    {
        return ApplyAlignment(AlignmentOperation.Left, "Aligned selected entities left.");
    }

    public ToolResult AlignSelectionRight()
    {
        return ApplyAlignment(AlignmentOperation.Right, "Aligned selected entities right.");
    }

    public ToolResult AlignSelectionTop()
    {
        return ApplyAlignment(AlignmentOperation.Top, "Aligned selected entities top.");
    }

    public ToolResult AlignSelectionBottom()
    {
        return ApplyAlignment(AlignmentOperation.Bottom, "Aligned selected entities bottom.");
    }

    private ToolResult ApplyAlignment(
        AlignmentOperation operation,
        string successMessage)
    {
        if (_context.SelectionSet.IsEmpty)
        {
            return ToolResult.None("Select at least two entities to align.");
        }

        List<EntityId> selectedIds = _context.SelectionSet.SelectedIds.ToList();

        var service = new AlignmentService();
        IReadOnlyList<CadEntity> replacements = service.CreateAlignedEntities(
            _context.Document,
            selectedIds,
            operation);

        if (replacements.Count == 0)
        {
            return ToolResult.None("Select at least two movable entities to align.");
        }

        _context.CommandHistory.Execute(
            _context.Document,
            new ReplaceEntitiesCommand(replacements, markDimensionsStale: true));

        _context.SelectionSet.ReplaceWith(
            selectedIds.Where(id =>
                _context.Document.Entities.TryGet(id, out CadEntity? entity) &&
                entity is not null &&
                _context.Document.IsEntitySelectable(entity)));

        return ToolResult.Completed(successMessage);
    }



    public ToolResult DistributeSelectionHorizontally()
    {
        return ApplyDistribution(
            DistributionOperation.Horizontal,
            "Distributed selected entities horizontally.");
    }

    public ToolResult DistributeSelectionVertically()
    {
        return ApplyDistribution(
            DistributionOperation.Vertical,
            "Distributed selected entities vertically.");
    }

    private ToolResult ApplyDistribution(
        DistributionOperation operation,
        string successMessage)
    {
        if (_context.SelectionSet.IsEmpty)
        {
            return ToolResult.None("Select at least three entities to distribute.");
        }

        List<EntityId> selectedIds = _context.SelectionSet.SelectedIds.ToList();

        var service = new DistributionService();
        IReadOnlyList<CadEntity> replacements = service.CreateDistributedEntities(
            _context.Document,
            selectedIds,
            operation);

        if (replacements.Count == 0)
        {
            return ToolResult.None("Select at least three movable entities to distribute.");
        }

        _context.CommandHistory.Execute(
            _context.Document,
            new ReplaceEntitiesCommand(replacements, markDimensionsStale: true));

        _context.SelectionSet.ReplaceWith(
            selectedIds.Where(id =>
                _context.Document.Entities.TryGet(id, out CadEntity? entity) &&
                entity is not null &&
                _context.Document.IsEntitySelectable(entity)));

        return ToolResult.Completed(successMessage);
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