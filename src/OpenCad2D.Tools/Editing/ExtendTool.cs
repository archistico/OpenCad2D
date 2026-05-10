using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Extends a line entity until it reaches a selected line boundary.
/// </summary>
public sealed class ExtendTool : ICadTool
{
    private EntityId? _boundaryEntityId;
    private LineEntity? _boundaryLine;
    private EntityId? _previewTargetEntityId;
    private LineEntity? _previewLine;

    public string Name => "Extend";

    public ExtendToolState State { get; private set; } =
        ExtendToolState.WaitingForBoundaryEntity;

    public EntityId? BoundaryEntityId => _boundaryEntityId;

    public bool HasPreview =>
        State == ExtendToolState.WaitingForTargetEntity &&
        _previewLine is not null;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            ExtendToolState.WaitingForBoundaryEntity =>
                AcceptBoundaryEntity(context, pointer),

            ExtendToolState.WaitingForTargetEntity =>
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

        if (State != ExtendToolState.WaitingForTargetEntity ||
            _boundaryLine is null)
        {
            return ToolResult.None();
        }

        UpdatePreview(context, pointer.ModelPoint);

        return HasPreview
            ? ToolResult.Updated("Extend preview updated.")
            : ToolResult.None("Select a line endpoint that can be extended to the boundary.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Extend command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Extend tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewLine is null
            ? Array.Empty<CadEntity>()
            : new CadEntity[] { _previewLine };
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
            return ToolResult.None("Extend currently supports line boundaries only.");
        }

        if (!context.Document.IsEntityVisible(line))
        {
            return ToolResult.None("Boundary entity is not visible.");
        }

        _boundaryEntityId = line.Id;
        _boundaryLine = line;
        _previewTargetEntityId = null;
        _previewLine = null;
        State = ExtendToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = line.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Started(
            "Select a line to extend to the boundary.");
    }

    private ToolResult AcceptTargetEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_boundaryLine is null)
        {
            throw new InvalidOperationException(
                "Cannot extend before selecting a boundary line.");
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select a line to extend.");
        }

        if (selectedId.Value.Equals(_boundaryLine.Id))
        {
            return ToolResult.None("Target line must be different from the boundary line.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (entity is not LineEntity targetLine)
        {
            return ToolResult.None("Extend currently supports line targets only.");
        }

        if (!context.Document.IsEntitySelectable(targetLine))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        LineEntity? extendedLine = LineExtendService.ExtendToBoundary(
            targetLine,
            _boundaryLine,
            pointer.ModelPoint,
            context.GeometryTolerance);

        if (extendedLine is null)
        {
            return ToolResult.None(
                "The selected line cannot be extended to the boundary from the picked side.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { targetLine },
                new[] { extendedLine },
                "Extend line"));

        _previewTargetEntityId = null;
        _previewLine = null;
        context.CurrentBasePoint = targetLine.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Completed(
            "Line extended. Select another line to extend, or press Escape.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_boundaryLine is null)
        {
            _previewTargetEntityId = null;
            _previewLine = null;
            return;
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            point);

        if (selectedId is null ||
            selectedId.Value.Equals(_boundaryLine.Id))
        {
            _previewTargetEntityId = null;
            _previewLine = null;
            return;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (entity is not LineEntity targetLine ||
            !context.Document.IsEntitySelectable(targetLine))
        {
            _previewTargetEntityId = null;
            _previewLine = null;
            return;
        }

        _previewTargetEntityId = targetLine.Id;
        _previewLine = LineExtendService.ExtendToBoundary(
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
        _previewTargetEntityId = null;
        _previewLine = null;
        State = ExtendToolState.WaitingForBoundaryEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
