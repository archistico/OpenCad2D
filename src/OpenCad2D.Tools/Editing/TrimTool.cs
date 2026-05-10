using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Trims a line entity against a selected line boundary.
/// </summary>
public sealed class TrimTool : ICadTool
{
    private EntityId? _boundaryEntityId;
    private LineEntity? _boundaryLine;
    private IReadOnlyList<LineEntity> _previewLines = Array.Empty<LineEntity>();

    public string Name => "Trim";

    public TrimToolState State { get; private set; } =
        TrimToolState.WaitingForBoundaryEntity;

    public EntityId? BoundaryEntityId => _boundaryEntityId;

    public bool HasPreview =>
        State == TrimToolState.WaitingForTargetEntity &&
        _previewLines.Count > 0;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            TrimToolState.WaitingForBoundaryEntity =>
                AcceptBoundaryEntity(context, pointer),

            TrimToolState.WaitingForTargetEntity =>
                AcceptTargetEntity(context, pointer),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != TrimToolState.WaitingForTargetEntity ||
            _boundaryLine is null)
        {
            return ToolResult.None();
        }

        UpdatePreview(context, pointer.ModelPoint);

        return HasPreview
            ? ToolResult.Updated("Trim preview updated.")
            : ToolResult.None("Select a line segment that can be trimmed by the boundary.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Trim command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Trim tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewLines
            .Cast<CadEntity>()
            .ToList();
    }

    private ToolResult AcceptBoundaryEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        EntityId? selectedId = SelectEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select a line boundary.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (entity is not LineEntity line)
        {
            return ToolResult.None("Trim currently supports line boundaries only.");
        }

        if (!context.Document.IsEntityVisible(line))
        {
            return ToolResult.None("Boundary entity is not visible.");
        }

        _boundaryEntityId = line.Id;
        _boundaryLine = line;
        _previewLines = Array.Empty<LineEntity>();
        State = TrimToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = line.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Started(
            "Select a line side to trim by the boundary.");
    }

    private ToolResult AcceptTargetEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_boundaryLine is null)
        {
            throw new InvalidOperationException(
                "Cannot trim before selecting a boundary line.");
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select a line to trim.");
        }

        if (selectedId.Value.Equals(_boundaryLine.Id))
        {
            return ToolResult.None("Target line must be different from the boundary line.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (entity is not LineEntity targetLine)
        {
            return ToolResult.None("Trim currently supports line targets only.");
        }

        if (!context.Document.IsEntitySelectable(targetLine))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        IReadOnlyList<LineEntity> trimmedLines = LineTrimService.TrimByBoundary(
            targetLine,
            _boundaryLine,
            pointer.ModelPoint,
            context.GeometryTolerance);

        if (trimmedLines.Count == 0)
        {
            return ToolResult.None(
                "The selected line cannot be trimmed by the boundary from the picked side.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { targetLine },
                trimmedLines,
                "Trim line"));

        _previewLines = Array.Empty<LineEntity>();
        context.CurrentBasePoint = targetLine.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Completed(
            "Line trimmed. Select another line to trim, or press Escape.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_boundaryLine is null)
        {
            _previewLines = Array.Empty<LineEntity>();
            return;
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            point);

        if (selectedId is null ||
            selectedId.Value.Equals(_boundaryLine.Id))
        {
            _previewLines = Array.Empty<LineEntity>();
            return;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (entity is not LineEntity targetLine ||
            !context.Document.IsEntitySelectable(targetLine))
        {
            _previewLines = Array.Empty<LineEntity>();
            return;
        }

        _previewLines = LineTrimService.TrimByBoundary(
            targetLine,
            _boundaryLine,
            point,
            context.GeometryTolerance);
    }

    private static EntityId? SelectEntityByPoint(
        ToolContext context,
        Point2D point)
    {
        return context.Selection.Service.SelectByPoint(
            context.Document,
            point,
            context.Selection.Tolerance);
    }

    private void Reset(ToolContext? context = null)
    {
        _boundaryEntityId = null;
        _boundaryLine = null;
        _previewLines = Array.Empty<LineEntity>();
        State = TrimToolState.WaitingForBoundaryEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
