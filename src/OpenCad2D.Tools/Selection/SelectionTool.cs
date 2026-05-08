using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Selection;

/// <summary>
/// Interactive tool used to select entities by point or by window.
/// </summary>
public sealed class SelectionTool : ICadTool
{
    private Point2D? _dragStartPoint;
    private Point2D? _dragCurrentPoint;
    private bool _isDraggingWindow;

    public string Name => "Selection";

    public Point2D? DragStartPoint => _dragStartPoint;

    public Point2D? DragCurrentPoint => _dragCurrentPoint;

    public bool HasWindowPreview =>
        _dragStartPoint.HasValue &&
        _dragCurrentPoint.HasValue &&
        _isDraggingWindow;

    public WindowSelectionMode? CurrentWindowMode
    {
        get
        {
            if (!HasWindowPreview ||
                _dragStartPoint is null ||
                _dragCurrentPoint is null)
            {
                return null;
            }

            return GetWindowMode(
                _dragStartPoint.Value,
                _dragCurrentPoint.Value);
        }
    }

    public BoundingBox2D? GetPreviewWindow()
    {
        if (!HasWindowPreview ||
            _dragStartPoint is null ||
            _dragCurrentPoint is null)
        {
            return null;
        }

        return BoundingBox2D.FromPoints(
            _dragStartPoint.Value,
            _dragCurrentPoint.Value);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        _dragStartPoint = pointer.ModelPoint;
        _dragCurrentPoint = pointer.ModelPoint;
        _isDraggingWindow = false;

        return SelectByPoint(
            context,
            pointer);
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (_dragStartPoint is null)
        {
            return ToolResult.None();
        }

        _dragCurrentPoint = pointer.ModelPoint;

        if (!_isDraggingWindow &&
            ShouldStartWindowSelection(
                _dragStartPoint.Value,
                pointer.ModelPoint,
                context.SelectionDragThreshold))
        {
            _isDraggingWindow = true;
        }

        return _isDraggingWindow
            ? ToolResult.Updated("Selection window updated.")
            : ToolResult.None();
    }

    public ToolResult OnPointerReleased(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (!HasWindowPreview ||
            _dragStartPoint is null)
        {
            ResetDragState();

            return ToolResult.None();
        }

        Point2D firstPoint = _dragStartPoint.Value;
        Point2D secondPoint = pointer.ModelPoint;

        BoundingBox2D window = BoundingBox2D.FromPoints(
            firstPoint,
            secondPoint);

        WindowSelectionMode mode = GetWindowMode(
            firstPoint,
            secondPoint);

        IReadOnlyList<EntityId> selectedIds =
            context.SelectionService.SelectByWindow(
                context.Document,
                window,
                mode);

        if (pointer.IsShiftPressed)
        {
            foreach (EntityId id in selectedIds)
            {
                context.SelectionSet.Toggle(id);
            }
        }
        else
        {
            context.SelectionSet.ReplaceWith(selectedIds);
        }

        ResetDragState();

        return ToolResult.Updated(
            mode == WindowSelectionMode.Inside
                ? "Window selection completed."
                : "Crossing selection completed.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ResetDragState();
        context.SelectionSet.Clear();

        return ToolResult.Cancelled("Selection cleared.");
    }

    private static ToolResult SelectByPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        EntityId? selectedId = context.SelectionService.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.SelectionTolerance);

        if (selectedId is null)
        {
            if (!pointer.IsShiftPressed)
            {
                context.SelectionSet.Clear();

                return ToolResult.Updated("Selection cleared.");
            }

            return ToolResult.None("Nothing selected.");
        }

        if (pointer.IsShiftPressed)
        {
            context.SelectionSet.Toggle(selectedId.Value);

            return ToolResult.Updated("Selection toggled.");
        }

        context.SelectionSet.ReplaceWith(selectedId.Value);

        return ToolResult.Updated("Entity selected.");
    }

    private static bool ShouldStartWindowSelection(
        Point2D firstPoint,
        Point2D currentPoint,
        double threshold)
    {
        return firstPoint.DistanceTo(currentPoint) >= threshold;
    }

    private static WindowSelectionMode GetWindowMode(
        Point2D firstPoint,
        Point2D secondPoint)
    {
        return secondPoint.X >= firstPoint.X
            ? WindowSelectionMode.Inside
            : WindowSelectionMode.Crossing;
    }

    private void ResetDragState()
    {
        _dragStartPoint = null;
        _dragCurrentPoint = null;
        _isDraggingWindow = false;
    }
}