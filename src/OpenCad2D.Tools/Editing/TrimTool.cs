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
    private CadEntity? _previewTargetEntity;
    private Point2D? _previewPickPoint;
    private IReadOnlyList<CadEntity> _previewEntities = Array.Empty<CadEntity>();
    private IReadOnlyList<CadEntity> _highlightPreviewEntities = Array.Empty<CadEntity>();
    private readonly Stack<ICadCommand> _trimCommands = new();
    private readonly HashSet<EntityId> _selectedBoundaryIds = new();
    private CadEntity? _previewBoundaryCandidate;
    private string? _feedback;
    private bool _usesAllVisibleCuttingEdges;

    public string Name => "Trim";

    public bool IsCommandFinished { get; private set; }

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

        RefreshCuttingEdges(context);
        var options = new List<CommandOption>
        {
            new("All", "A", "Use all currently visible supported curves as cutting edges")
        };
        if (State == TrimToolState.WaitingForTargetEntity)
        {
            options.Add(new CommandOption("Edges", "E", "Choose cutting edges again without undoing completed trims"));
        }
        if (_trimCommands.Count > 0)
        {
            options.Add(new CommandOption("Undo", "U", "Undo the last trim made by this command"));
        }

        string edges = _usesAllVisibleCuttingEdges ? "all visible edges" : $"{_boundaryEntities.Count} cutting edges";
        string prompt = State == TrimToolState.WaitingForBoundaryEntity
            ? "1/2: Select a cutting edge, or Enter/All for all visible edges. Esc cancels."
            : $"2/2: Click the dashed part to REMOVE ({edges}). Ctrl-click adds an edge; E changes edges; Enter/right-click finishes.";
        if (State == TrimToolState.WaitingForTargetEntity && _boundaryEntities.Count == 0)
        {
            prompt = "2/2: No visible cutting edges remain. E selects new edges; A uses all visible edges; Enter finishes.";
        }
        return new CommandPromptState(
            "TRIM", prompt, CommandInputKind.SelectionOrOption, options,
            acceptsEmptyEnter: true,
            placeholder: State == TrimToolState.WaitingForBoundaryEntity
                ? "Click a cutting edge or press Enter for All"
                : "Click the part to remove; Ctrl-click adds edges; Enter finishes",
            feedback: _feedback);
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ToolResult result = HandleCommandInputCore(input, context);
        _feedback = result.Message;
        return result;
    }

    private ToolResult HandleCommandInputCore(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == TrimToolState.WaitingForBoundaryEntity && input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return UseAllVisibleCuttingEdges(context);
        }

        if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Edges", StringComparison.OrdinalIgnoreCase))
        {
            ClearCuttingEdges();
            State = TrimToolState.WaitingForBoundaryEntity;
            context.CurrentBasePoint = null;
            return ToolResult.Started("Choose a new cutting edge, or Enter for All. Completed trims are kept; U undoes a trim.");
        }

        if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "All", StringComparison.OrdinalIgnoreCase))
        {
            return UseAllVisibleCuttingEdges(context);
        }

        if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Undo", StringComparison.OrdinalIgnoreCase))
        {
            return UndoLastTrim(context);
        }

        if (State == TrimToolState.WaitingForTargetEntity &&
            input.Kind == CommandInputSubmissionKind.Confirm)
        {
            Reset(context);
            IsCommandFinished = true;
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
        ToolResult result = OnPointerPressedCore(context, pointer);
        if (result.Kind == ToolResultKind.None)
        {
            ClearPreview();
        }
        _feedback = result.Message;
        return result;
    }

    private ToolResult OnPointerPressedCore(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        RefreshCuttingEdges(context);

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

        RefreshCuttingEdges(context);
        if (State != TrimToolState.WaitingForTargetEntity)
        {
            return ToolResult.None();
        }

        if (pointer.IsControlPressed)
        {
            ClearPreview();
            EntityId? candidateId = SelectVisibleEntityByPoint(context, pointer.ModelPoint);
            _previewBoundaryCandidate = candidateId is null ? null : context.Document.Entities.GetRequired(candidateId.Value);
            _feedback = _usesAllVisibleCuttingEdges
                ? "All mode already includes visible cutting edges. Use E to choose specific edges."
                : "Ctrl-click to add a cutting edge; release Ctrl to preview the part to remove.";
            return ToolResult.Updated(_feedback);
        }

        UpdatePreview(context, pointer.ModelPoint);
        _feedback = HasPreview
            ? "Click the dashed portion to remove it. The solid preview will remain."
            : BuildTrimPreviewFailureMessage(context, pointer.ModelPoint);
        return HasPreview ? ToolResult.Updated(_feedback) : ToolResult.None(_feedback);
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

        RefreshCuttingEdges(context);
        return new ToolPreviewDescriptor(
            entities: GetPreviewEntities(),
            highlightedEntities: GetHighlightedPreviewEntities(),
            highlightedEntityKind: ToolPreviewHighlightKind.Removal,
            markers: GetPreviewMarkers(),
            entityOverlays: GetPreviewEntityOverlays());
    }

    private IReadOnlyList<ToolPreviewMarker> GetPreviewMarkers()
    {
        if (_previewPickPoint is null || _highlightPreviewEntities.Count == 0)
        {
            return Array.Empty<ToolPreviewMarker>();
        }

        return new[]
        {
            new ToolPreviewMarker(
                _previewPickPoint.Value,
                ToolPreviewMarkerKind.Hot,
                ToolPreviewMarkerShape.Circle)
        };
    }

    private IReadOnlyList<ToolPreviewEntityOverlay> GetPreviewEntityOverlays()
    {
        var overlays = new List<ToolPreviewEntityOverlay>();

        if (_boundaryEntities.Count > 0)
        {
            overlays.Add(new ToolPreviewEntityOverlay(
                _boundaryEntities,
                ToolPreviewHighlightKind.Emphasis));
        }

        if (_previewTargetEntity is not null && _highlightPreviewEntities.Count > 0)
        {
            overlays.Add(new ToolPreviewEntityOverlay(
                new[] { _previewTargetEntity },
                ToolPreviewHighlightKind.Emphasis));
        }

        if (_previewBoundaryCandidate is not null)
        {
            overlays.Add(new ToolPreviewEntityOverlay(new[] { _previewBoundaryCandidate }));
        }

        return overlays;
    }


    private string BuildTrimPreviewFailureMessage(
        ToolContext context,
        Point2D point)
    {
        EntityId? selectedId = SelectEntityByPoint(
            context,
            point);

        if (selectedId is null)
        {
            return "Select a removable side of an entity that intersects the cutting edge.";
        }

        if (!_usesAllVisibleCuttingEdges &&
            _boundaryEntities.Any(boundary => selectedId.Value.Equals(boundary.Id)))
        {
            return "Target entity must be different from the selected cutting edge.";
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedEntity(entity))
        {
            return "Trim supports lines, circles, arcs, ellipses, elliptical arcs, polylines and splines.";
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return "Target entity is not editable.";
        }

        IReadOnlyList<CadEntity> effectiveBoundaries = GetEffectiveBoundariesForTarget(entity);

        if (effectiveBoundaries.Count == 0)
        {
            return "No cutting edge is available for the selected target entity.";
        }

        return EditingStatusMessageBuilder.BuildTrimFailureMessage(
            entity,
            effectiveBoundaries,
            point,
            context.GeometryTolerance);
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
        _selectedBoundaryIds.Clear();
        _selectedBoundaryIds.Add(entity.Id);
        _boundaryEntities.Clear();
        _boundaryEntities.Add(entity);
        _usesAllVisibleCuttingEdges = false;
        _secondBoundaryEntityId = null;
        ClearPreview();
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
            ClearPreview();
            return ToolResult.None("No visible cutting edges remain. Use E to select new edges or A for All.");
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
                EditingStatusMessageBuilder.BuildTrimFailureMessage(
                    entity,
                    effectiveBoundaries,
                    pointer.ModelPoint,
                    context.GeometryTolerance));
        }

        var command = new ModifyEntitiesCommand(new[] { entity }, trimmedEntities, "Trim entity");
        context.Commands.Execute(context.Document, command);
        _trimCommands.Push(command);
        RefreshCuttingEdges(context);

        ClearPreview();
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
        _selectedBoundaryIds.Add(entity.Id);
        _usesAllVisibleCuttingEdges = false;
        _secondBoundaryEntityId = entity.Id;
        ClearPreview();
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

        _selectedBoundaryIds.Clear();
        _boundaryEntities.Clear();
        _boundaryEntities.AddRange(cuttingEdges);
        _usesAllVisibleCuttingEdges = true;
        _boundaryEntity = cuttingEdges[0];
        _boundaryEntityId = cuttingEdges[0].Id;
        _secondBoundaryEntityId = cuttingEdges.Count > 1
            ? cuttingEdges[1].Id
            : null;
        ClearPreview();
        State = TrimToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = null;

        return ToolResult.Started(
            $"Using all visible supported entities as cutting edges ({cuttingEdges.Count}). Select the side to remove; dashed preview shows the trimmed portion.");
    }

    private ToolResult UndoLastTrim(ToolContext context)
    {
        ICadCommand? nextUndo = context.Commands.History.NextUndoCommand;
        if (nextUndo is null || !_trimCommands.Contains(nextUndo))
        {
            return ToolResult.None("No trim from this command is next to undo. U will not undo other actions; use global Undo if needed.");
        }

        context.Commands.History.Undo(context.Document);
        while (_trimCommands.Pop() != nextUndo)
        {
            // Discard trim entries already undone through the global history.
        }
        ClearPreview();
        RefreshCuttingEdges(context);
        context.CurrentBasePoint = null;
        return ToolResult.Updated("Last trim undone. Click another dashed part to remove, or E to change cutting edges.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        _previewBoundaryCandidate = null;
        if (_boundaryEntity is null)
        {
            ClearPreview();
            return;
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            point);

        if (selectedId is null ||
            (!_usesAllVisibleCuttingEdges &&
                _boundaryEntities.Any(boundary => selectedId.Value.Equals(boundary.Id))))
        {
            ClearPreview();
            return;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedEntity(entity) ||
            !context.Document.IsEntitySelectable(entity))
        {
            ClearPreview();
            return;
        }

        IReadOnlyList<CadEntity> effectiveBoundaries = GetEffectiveBoundariesForTarget(entity);

        if (effectiveBoundaries.Count == 0)
        {
            ClearPreview();
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
        _previewTargetEntity = _highlightPreviewEntities.Count == 0
            ? null
            : entity;
        _previewPickPoint = _highlightPreviewEntities.Count == 0
            ? null
            : entity.GetClosestPoint(point);
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
            .OrderBy(result => IsSupportedEntity(result.Entity) ? 0 : 1)
            .ThenBy(result => result.Distance)
            .ThenByDescending(result => result.Entity.DrawOrder)
            .Select(result => (EntityId?)result.Entity.Id)
            .FirstOrDefault();
    }

    private EntityId? SelectEntityByPoint(
        ToolContext context,
        Point2D point)
    {
        IReadOnlyList<EntityId> hits = context.Selection.Service.SelectAllByPoint(
            context.Document, point, context.Selection.Tolerance);
        EntityId? fallback = null;
        foreach (EntityId id in hits)
        {
            bool isBoundary = !_usesAllVisibleCuttingEdges && _selectedBoundaryIds.Contains(id);
            if (isBoundary)
            {
                continue;
            }
            fallback ??= id;
            if (IsSupportedEntity(context.Document.Entities.GetRequired(id)))
            {
                return id;
            }
        }
        return fallback ?? (hits.Count > 0 ? hits[0] : null);
    }


    private static bool IsSupportedEntity(CadEntity entity)
    {
        return entity is LineEntity or CircleEntity or ArcEntity or EllipseEntity or EllipticalArcEntity or PolylineEntity or BezierSplineEntity;
    }

    private void RefreshCuttingEdges(ToolContext context)
    {
        IEnumerable<CadEntity> current = _usesAllVisibleCuttingEdges
            ? context.Document.Entities.All.Where(entity => IsSupportedEntity(entity) && context.Document.IsEntityVisible(entity))
            : _selectedBoundaryIds
                .Select(id => context.Document.Entities.TryGet(id, out CadEntity? entity) ? entity : null)
                .OfType<CadEntity>()
                .Where(entity => IsSupportedEntity(entity) && context.Document.IsEntityVisible(entity));
        List<CadEntity> refreshed = current.ToList();
        if (!_boundaryEntities.SequenceEqual(refreshed) ||
            (_previewTargetEntity is not null &&
            (!context.Document.Entities.TryGet(_previewTargetEntity.Id, out CadEntity? target) ||
             !ReferenceEquals(target, _previewTargetEntity) || !context.Document.IsEntitySelectable(target))))
        {
            ClearPreview();
        }
        _boundaryEntities.Clear();
        _boundaryEntities.AddRange(refreshed);
        _boundaryEntity = _boundaryEntities.FirstOrDefault();
        _boundaryEntityId = _boundaryEntity?.Id;
        _secondBoundaryEntityId = _boundaryEntities.Count > 1 ? _boundaryEntities[^1].Id : null;
    }

    private void ClearPreview()
    {
        _previewEntities = Array.Empty<CadEntity>();
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        _previewTargetEntity = null;
        _previewPickPoint = null;
        _previewBoundaryCandidate = null;
    }

    private void ClearCuttingEdges()
    {
        _boundaryEntityId = null;
        _secondBoundaryEntityId = null;
        _boundaryEntity = null;
        _boundaryEntities.Clear();
        _selectedBoundaryIds.Clear();
        _usesAllVisibleCuttingEdges = false;
        ClearPreview();
    }

    private void Reset(ToolContext? context = null)
    {
        ClearCuttingEdges();
        _trimCommands.Clear();
        _feedback = null;
        IsCommandFinished = false;
        State = TrimToolState.WaitingForBoundaryEntity;
        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
