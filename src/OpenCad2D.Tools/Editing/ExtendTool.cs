using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Extends editable entities until they reach a selected boundary entity.
/// </summary>
public sealed class ExtendTool : ICadTool
{
    private EntityId? _boundaryEntityId;
    private CadEntity? _boundaryEntity;
    private EntityId? _previewTargetEntityId;
    private CadEntity? _previewEntity;

    public string Name => "Extend";

    public ExtendToolState State { get; private set; } =
        ExtendToolState.WaitingForBoundaryEntity;

    public EntityId? BoundaryEntityId => _boundaryEntityId;

    public bool HasPreview =>
        State == ExtendToolState.WaitingForTargetEntity &&
        _previewEntity is not null;

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
            _boundaryEntity is null)
        {
            return ToolResult.None();
        }

        UpdatePreview(context, pointer.ModelPoint);

        return HasPreview
            ? ToolResult.Updated("Extend preview updated.")
            : ToolResult.None("Select an entity endpoint that can be extended to the boundary.");
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
        return _previewEntity is null
            ? Array.Empty<CadEntity>()
            : new[] { _previewEntity };
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
            return ToolResult.None("Select a boundary entity.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedBoundaryEntity(entity))
        {
            return ToolResult.None("Extend supports line, circle, arc and polyline boundaries.");
        }

        if (!context.Document.IsEntityVisible(entity))
        {
            return ToolResult.None("Boundary entity is not visible.");
        }

        _boundaryEntityId = entity.Id;
        _boundaryEntity = entity;
        _previewTargetEntityId = null;
        _previewEntity = null;
        State = ExtendToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Started(
            "Select an entity to extend to the boundary.");
    }

    private ToolResult AcceptTargetEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_boundaryEntity is null)
        {
            throw new InvalidOperationException(
                "Cannot extend before selecting a boundary entity.");
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select an entity to extend.");
        }

        if (selectedId.Value.Equals(_boundaryEntity.Id))
        {
            return ToolResult.None("Target entity must be different from the boundary entity.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedTargetEntity(entity))
        {
            return ToolResult.None("Extend supports lines, arcs and open polylines as targets.");
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        CadEntity? extendedEntity = CadExtendService.ExtendToBoundary(
            entity,
            _boundaryEntity,
            pointer.ModelPoint,
            context.GeometryTolerance);

        if (extendedEntity is null)
        {
            return ToolResult.None(
                "The selected entity cannot be extended to the boundary from the picked side.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { entity },
                new[] { extendedEntity },
                "Extend entity"));

        _previewTargetEntityId = null;
        _previewEntity = null;
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Completed(
            "Entity extended. Select another entity to extend, or press Escape.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_boundaryEntity is null)
        {
            _previewTargetEntityId = null;
            _previewEntity = null;
            return;
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            point);

        if (selectedId is null ||
            selectedId.Value.Equals(_boundaryEntity.Id))
        {
            _previewTargetEntityId = null;
            _previewEntity = null;
            return;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedTargetEntity(entity) ||
            !context.Document.IsEntitySelectable(entity))
        {
            _previewTargetEntityId = null;
            _previewEntity = null;
            return;
        }

        _previewTargetEntityId = entity.Id;
        _previewEntity = CadExtendService.ExtendToBoundary(
            entity,
            _boundaryEntity,
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


    private static bool IsSupportedBoundaryEntity(CadEntity entity)
    {
        return entity is LineEntity or CircleEntity or ArcEntity or PolylineEntity;
    }

    private static bool IsSupportedTargetEntity(CadEntity entity)
    {
        return entity is LineEntity or ArcEntity or PolylineEntity { IsClosed: false };
    }

    private void Reset(ToolContext? context = null)
    {
        _boundaryEntityId = null;
        _boundaryEntity = null;
        _previewTargetEntityId = null;
        _previewEntity = null;
        State = ExtendToolState.WaitingForBoundaryEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
