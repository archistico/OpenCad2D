using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Trims editable entities against a selected boundary entity.
/// </summary>
public sealed class TrimTool : ICadTool
{
    private EntityId? _boundaryEntityId;
    private CadEntity? _boundaryEntity;
    private IReadOnlyList<CadEntity> _previewEntities = Array.Empty<CadEntity>();
    private IReadOnlyList<CadEntity> _highlightPreviewEntities = Array.Empty<CadEntity>();

    public string Name => "Trim";

    public TrimToolState State { get; private set; } =
        TrimToolState.WaitingForBoundaryEntity;

    public EntityId? BoundaryEntityId => _boundaryEntityId;

    public bool HasPreview =>
        State == TrimToolState.WaitingForTargetEntity &&
        _previewEntities.Count > 0;

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
            _boundaryEntity is null)
        {
            return ToolResult.None();
        }

        UpdatePreview(context, pointer.ModelPoint);

        return HasPreview
            ? ToolResult.Updated("Trim preview updated.")
            : ToolResult.None("Select an entity part that can be trimmed by the boundary.");
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
        return _previewEntities.ToList();
    }

    /// <summary>
    /// Gets the entities that represent the portion that will be removed by the current trim preview.
    /// </summary>
    public IReadOnlyList<CadEntity> GetHighlightedPreviewEntities()
    {
        return _highlightPreviewEntities.ToList();
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

        if (!IsSupportedEntity(entity))
        {
            return ToolResult.None("Trim supports lines, circles, arcs and polylines.");
        }

        if (!context.Document.IsEntityVisible(entity))
        {
            return ToolResult.None("Boundary entity is not visible.");
        }

        _boundaryEntityId = entity.Id;
        _boundaryEntity = entity;
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        State = TrimToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Started(
            "Select an entity side to trim by the boundary.");
    }

    private ToolResult AcceptTargetEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_boundaryEntity is null)
        {
            throw new InvalidOperationException(
                "Cannot trim before selecting a boundary entity.");
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select an entity to trim.");
        }

        if (selectedId.Value.Equals(_boundaryEntity.Id))
        {
            return ToolResult.None("Target entity must be different from the boundary entity.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedEntity(entity))
        {
            return ToolResult.None("Trim supports lines, circles, arcs and polylines.");
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        IReadOnlyList<CadEntity> trimmedEntities = CadTrimService.TrimByBoundary(
            entity,
            _boundaryEntity,
            pointer.ModelPoint,
            context.GeometryTolerance);

        if (trimmedEntities.Count == 0)
        {
            return ToolResult.None(
                "The selected entity cannot be trimmed by the boundary from the picked side.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { entity },
                trimmedEntities,
                "Trim entity"));

        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Completed(
            "Entity trimmed. Select another entity to trim, or press Escape.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_boundaryEntity is null)
        {
            _previewEntities = Array.Empty<CadEntity>();
            _highlightPreviewEntities = Array.Empty<CadEntity>();
            return;
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            point);

        if (selectedId is null ||
            selectedId.Value.Equals(_boundaryEntity.Id))
        {
            _previewEntities = Array.Empty<CadEntity>();
            _highlightPreviewEntities = Array.Empty<CadEntity>();
            return;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedEntity(entity) ||
            !context.Document.IsEntitySelectable(entity))
        {
            _previewEntities = Array.Empty<CadEntity>();
            _highlightPreviewEntities = Array.Empty<CadEntity>();
            return;
        }

        _previewEntities = CadTrimService.TrimByBoundary(
            entity,
            _boundaryEntity,
            point,
            context.GeometryTolerance);
        _highlightPreviewEntities = CreateTrimHighlightEntities(
            entity,
            _previewEntities,
            context.GeometryTolerance);
    }


    private static IReadOnlyList<CadEntity> CreateTrimHighlightEntities(
        CadEntity target,
        IReadOnlyList<CadEntity> keptEntities,
        GeometryTolerance tolerance)
    {
        if (target is not LineEntity sourceLine)
        {
            return Array.Empty<CadEntity>();
        }

        List<LineEntity> keptLines = keptEntities
            .OfType<LineEntity>()
            .ToList();

        if (keptLines.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        if (keptLines.Count == 1)
        {
            LineEntity kept = keptLines[0];

            if (tolerance.ArePointsEqual(kept.Start, sourceLine.Start))
            {
                return CreateHighlightLine(
                    sourceLine,
                    kept.End,
                    sourceLine.End,
                    tolerance);
            }

            if (tolerance.ArePointsEqual(kept.End, sourceLine.End))
            {
                return CreateHighlightLine(
                    sourceLine,
                    sourceLine.Start,
                    kept.Start,
                    tolerance);
            }
        }

        if (keptLines.Count >= 2)
        {
            LineEntity first = keptLines
                .OrderBy(line => LineParameterService.GetParameter(
                    sourceLine.Geometry,
                    line.Start,
                    tolerance))
                .First();
            LineEntity last = keptLines
                .OrderBy(line => LineParameterService.GetParameter(
                    sourceLine.Geometry,
                    line.End,
                    tolerance))
                .Last();

            return CreateHighlightLine(
                sourceLine,
                first.End,
                last.Start,
                tolerance);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreateHighlightLine(
        LineEntity source,
        Point2D start,
        Point2D end,
        GeometryTolerance tolerance)
    {
        if (start.DistanceTo(end) <= tolerance.Distance)
        {
            return Array.Empty<CadEntity>();
        }

        return new[]
        {
            new LineEntity(
                start,
                end,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
        };
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


    private static bool IsSupportedEntity(CadEntity entity)
    {
        return entity is LineEntity or CircleEntity or ArcEntity or PolylineEntity;
    }

    private void Reset(ToolContext? context = null)
    {
        _boundaryEntityId = null;
        _boundaryEntity = null;
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        State = TrimToolState.WaitingForBoundaryEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
