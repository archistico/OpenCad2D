using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Trims editable entities against a selected boundary entity.
/// </summary>
public sealed class TrimTool : ICadTool, ISnapModeProvider, ICommandDrivenTool, IToolPreviewDescriptorProvider
{
    private readonly List<CadEntity> _boundaryEntities = new();
    private EntityId? _boundaryEntityId;
    private EntityId? _secondBoundaryEntityId;
    private CadEntity? _boundaryEntity;
    private IReadOnlyList<CadEntity> _previewEntities = Array.Empty<CadEntity>();
    private IReadOnlyList<CadEntity> _highlightPreviewEntities = Array.Empty<CadEntity>();
    private int _trimOperationsExecuted;
    private bool _usesAllVisibleCuttingEdges;

    public string Name => "Trim";

    public TrimToolState State { get; private set; } =
        TrimToolState.WaitingForBoundaryEntity;

    public EntityId? BoundaryEntityId => _boundaryEntityId;

    /// <summary>
    /// Gets the optional second cutting edge selected with Ctrl-click.
    /// </summary>
    public EntityId? SecondBoundaryEntityId => _secondBoundaryEntityId;

    public bool HasPreview =>
        State == TrimToolState.WaitingForTargetEntity &&
        _previewEntities.Count > 0;


    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
    }


    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            TrimToolState.WaitingForBoundaryEntity => new CommandPromptState(
                "TRIM",
                "Select cutting edge",
                CommandInputKind.SelectionOrOption,
                new[]
                {
                    new CommandOption("All", "A", "Use all visible supported entities as cutting edges")
                },
                placeholder: "Click a cutting edge or type All"),

            TrimToolState.WaitingForTargetEntity => new CommandPromptState(
                "TRIM",
                "Select entity side to trim",
                CommandInputKind.SelectionOrOption,
                new[]
                {
                    new CommandOption("All", "A", "Use all visible supported entities as cutting edges"),
                    new CommandOption("Undo", "U", "Undo the last trim made by this command")
                },
                acceptsEmptyEnter: true,
                placeholder: "Click side to remove, Ctrl-click to add cutting edge, U to undo"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "All", StringComparison.OrdinalIgnoreCase))
        {
            return UseAllVisibleCuttingEdges(context);
        }

        if (State == TrimToolState.WaitingForTargetEntity &&
            input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Undo", StringComparison.OrdinalIgnoreCase))
        {
            return UndoLastTrim(context);
        }

        if (State == TrimToolState.WaitingForTargetEntity &&
            input.Kind == CommandInputSubmissionKind.Confirm)
        {
            Reset(context);
            return ToolResult.Completed("Trim command finished.");
        }

        return State == TrimToolState.WaitingForBoundaryEntity
            ? ToolResult.None("Select a cutting edge from the drawing canvas or type All.")
            : ToolResult.None("Select an entity side to trim from the drawing canvas, Ctrl-click to add cutting edges, or type Undo.");
    }

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
            _boundaryEntities.Count == 0)
        {
            return ToolResult.None();
        }

        UpdatePreview(context, pointer.ModelPoint);

        return HasPreview
            ? ToolResult.Updated("Trim preview updated. Dashed portion will be removed.")
            : ToolResult.None("Select a removable side of an entity that intersects the cutting edge.");
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


    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ToolPreviewDescriptor(
            entities: GetPreviewEntities(),
            highlightedEntities: GetHighlightedPreviewEntities(),
            highlightedEntityKind: ToolPreviewHighlightKind.Removal,
            entityOverlays: GetSelectedBoundaryOverlays());
    }

    private IReadOnlyList<ToolPreviewEntityOverlay> GetSelectedBoundaryOverlays()
    {
        if (_boundaryEntities.Count == 0)
        {
            return Array.Empty<ToolPreviewEntityOverlay>();
        }

        return new[]
        {
            new ToolPreviewEntityOverlay(
                _boundaryEntities,
                ToolPreviewHighlightKind.Emphasis)
        };
    }

    private ToolResult AcceptBoundaryEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        EntityId? selectedId = SelectVisibleEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select a visible boundary entity.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedEntity(entity))
        {
            return ToolResult.None("Trim supports lines, circles, arcs, ellipses, elliptical arcs, polylines and splines.");
        }

        if (!context.Document.IsEntityVisible(entity))
        {
            return ToolResult.None("Boundary entity is not visible.");
        }

        _boundaryEntityId = entity.Id;
        _boundaryEntity = entity;
        _boundaryEntities.Clear();
        _boundaryEntities.Add(entity);
        _usesAllVisibleCuttingEdges = false;
        _secondBoundaryEntityId = null;
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        State = TrimToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Started(
            "Select the side to remove. The dashed preview shows the portion that will be trimmed, or Ctrl-click another cutting edge.");
    }

    private ToolResult AcceptTargetEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_boundaryEntities.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot trim before selecting a boundary entity.");
        }

        if (pointer.IsControlPressed)
        {
            EntityId? visibleBoundaryId = SelectVisibleEntityByPoint(
                context,
                pointer.ModelPoint);

            if (visibleBoundaryId is null)
            {
                return ToolResult.None("Select a visible second cutting edge.");
            }

            CadEntity visibleBoundary = context.Document.Entities.GetRequired(visibleBoundaryId.Value);

            return AcceptSecondBoundaryEntity(context, visibleBoundary, pointer);
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select an editable entity to trim.");
        }

        if (!_usesAllVisibleCuttingEdges &&
            _boundaryEntities.Any(boundary => selectedId.Value.Equals(boundary.Id)))
        {
            return ToolResult.None("Target entity must be different from the selected cutting edge.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedEntity(entity))
        {
            return ToolResult.None("Trim supports lines, circles, arcs, ellipses, elliptical arcs, polylines and splines.");
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        IReadOnlyList<CadEntity> effectiveBoundaries = GetEffectiveBoundariesForTarget(entity);

        if (effectiveBoundaries.Count == 0)
        {
            return ToolResult.None("No cutting edge is available for the selected target entity.");
        }

        IReadOnlyList<CadEntity> trimmedEntities = CadTrimService.TrimByBoundaries(
            entity,
            effectiveBoundaries,
            pointer.ModelPoint,
            context.GeometryTolerance);

        if (trimmedEntities.Count == 0)
        {
            return ToolResult.None(
                _boundaryEntities.Count > 1
                    ? "No removable interval was found from the picked side. Pick a side between or outside the selected cutting edges."
                    : "No removable interval was found from the picked side. Pick the side that crosses the cutting edge.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { entity },
                trimmedEntities,
                "Trim entity"));
        _trimOperationsExecuted++;

        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Completed(
            "Entity trimmed. Select another entity to trim, type Undo, or press Enter/right-click/Escape to finish.");
    }

    private ToolResult AcceptSecondBoundaryEntity(
        ToolContext context,
        CadEntity entity,
        PointerInfo pointer)
    {
        if (_boundaryEntities.Any(boundary => boundary.Id.Equals(entity.Id)))
        {
            return ToolResult.None("Cutting edge is already selected.");
        }

        if (!IsSupportedEntity(entity))
        {
            return ToolResult.None("Trim supports lines, circles, arcs, ellipses, elliptical arcs, polylines and splines as cutting edges.");
        }

        if (!context.Document.IsEntityVisible(entity))
        {
            return ToolResult.None("Second cutting edge is not visible.");
        }

        _boundaryEntities.Add(entity);
        _usesAllVisibleCuttingEdges = false;
        _secondBoundaryEntityId = entity.Id;
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Started(
            $"Cutting edge selected ({_boundaryEntities.Count}). Select the side to remove; dashed preview shows the trimmed portion.");
    }

    private ToolResult UseAllVisibleCuttingEdges(ToolContext context)
    {
        List<CadEntity> cuttingEdges = context.Document.GetVisibleEntities()
            .Where(IsSupportedEntity)
            .ToList();

        if (cuttingEdges.Count == 0)
        {
            return ToolResult.None("No visible supported cutting edges found.");
        }

        _boundaryEntities.Clear();
        _boundaryEntities.AddRange(cuttingEdges);
        _usesAllVisibleCuttingEdges = true;
        _boundaryEntity = cuttingEdges[0];
        _boundaryEntityId = cuttingEdges[0].Id;
        _secondBoundaryEntityId = cuttingEdges.Count > 1
            ? cuttingEdges[1].Id
            : null;
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        State = TrimToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = null;

        return ToolResult.Started(
            $"Using all visible supported entities as cutting edges ({cuttingEdges.Count}). Select the side to remove; dashed preview shows the trimmed portion.");
    }

    private ToolResult UndoLastTrim(ToolContext context)
    {
        if (_trimOperationsExecuted <= 0)
        {
            return ToolResult.None("No trim operation to undo in the current command.");
        }

        if (!context.Commands.History.CanUndo)
        {
            _trimOperationsExecuted = 0;
            return ToolResult.None("No undoable trim operation is available.");
        }

        context.Commands.History.Undo(context.Document);
        _trimOperationsExecuted--;
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        context.CurrentBasePoint = null;

        return ToolResult.Updated("Last trim operation undone. Select another side to remove; dashed preview shows the trimmed portion.");
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
            (!_usesAllVisibleCuttingEdges &&
                _boundaryEntities.Any(boundary => selectedId.Value.Equals(boundary.Id))))
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

        IReadOnlyList<CadEntity> effectiveBoundaries = GetEffectiveBoundariesForTarget(entity);

        if (effectiveBoundaries.Count == 0)
        {
            _previewEntities = Array.Empty<CadEntity>();
            _highlightPreviewEntities = Array.Empty<CadEntity>();
            return;
        }

        _previewEntities = CadTrimService.TrimByBoundaries(
            entity,
            effectiveBoundaries,
            point,
            context.GeometryTolerance);
        _highlightPreviewEntities = CadTrimService.GetRemovedIntervalByBoundaries(
            entity,
            effectiveBoundaries,
            point,
            context.GeometryTolerance);
    }


    private IReadOnlyList<CadEntity> GetEffectiveBoundariesForTarget(CadEntity target)
    {
        return _usesAllVisibleCuttingEdges
            ? _boundaryEntities
                .Where(boundary => !boundary.Id.Equals(target.Id))
                .ToList()
            : _boundaryEntities;
    }


    private static EntityId? SelectVisibleEntityByPoint(
        ToolContext context,
        Point2D point)
    {
        BoundingBox2D searchArea = new(
            point.X - context.Selection.Tolerance,
            point.Y - context.Selection.Tolerance,
            point.X + context.Selection.Tolerance,
            point.Y + context.Selection.Tolerance);

        return context.Document.GetVisibleEntities(searchArea)
            .Select(entity => new
            {
                Entity = entity,
                Distance = point.DistanceTo(entity.GetClosestPoint(point))
            })
            .Where(result => result.Distance <= context.Selection.Tolerance)
            .OrderBy(result => result.Distance)
            .ThenByDescending(result => result.Entity.DrawOrder)
            .Select(result => (EntityId?)result.Entity.Id)
            .FirstOrDefault();
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
        return entity is LineEntity or CircleEntity or ArcEntity or EllipseEntity or EllipticalArcEntity or PolylineEntity or BezierSplineEntity;
    }

    private void Reset(ToolContext? context = null)
    {
        _boundaryEntityId = null;
        _secondBoundaryEntityId = null;
        _boundaryEntity = null;
        _boundaryEntities.Clear();
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        _trimOperationsExecuted = 0;
        _usesAllVisibleCuttingEdges = false;
        State = TrimToolState.WaitingForBoundaryEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
