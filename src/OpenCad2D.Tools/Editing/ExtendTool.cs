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
/// Extends native editable curves to the nearest selected boundary on the picked endpoint side.
/// </summary>
public sealed class ExtendTool : ICadTool, ICommandDrivenTool, IToolPreviewDescriptorProvider, ISnapModeProvider
{
    private readonly List<CadEntity> _boundaries = new();
    private readonly HashSet<EntityId> _selectedBoundaryIds = new();
    private readonly HashSet<ICadCommand> _extendCommands = new();
    private bool _usesAllBoundaries;
    private CadEntity? _previewTargetEntity;
    private CadEntity? _previewBoundaryCandidate;
    private Point2D? _previewEndpoint;
    private CadEntity? _previewEntity;
    private IReadOnlyList<CadEntity> _highlightPreviewEntities = Array.Empty<CadEntity>();
    private string? _feedback;

    public string Name => "Extend";
    public bool IsCommandFinished { get; private set; }
    public ExtendToolState State { get; private set; } = ExtendToolState.WaitingForBoundaryEntity;
    public EntityId? BoundaryEntityId => _boundaries.FirstOrDefault()?.Id;
    public bool HasPreview => State == ExtendToolState.WaitingForTargetEntity && _previewEntity is not null;

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SnapKind.EntityOnly;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        RefreshBoundaries(context);
        var options = new List<CommandOption>
        {
            new("All", "A", "Use all currently visible supported curves as boundaries")
        };
        if (State == ExtendToolState.WaitingForTargetEntity)
            options.Add(new CommandOption("Edges", "E", "Choose boundaries again; completed extensions are kept"));
        if (_extendCommands.Count > 0)
            options.Add(new CommandOption("Undo", "U", "Undo the last extension made by this command"));

        string edges = _usesAllBoundaries ? "all visible boundaries" : $"{_boundaries.Count} boundaries";
        string prompt = State == ExtendToolState.WaitingForBoundaryEntity
            ? "1/2: Select a boundary, or Enter/All for all visible boundaries. Esc cancels."
            : $"2/2: Click near the endpoint to EXTEND ({edges}). Ctrl-click adds a boundary; E changes boundaries; Enter/right-click finishes.";
        if (State == ExtendToolState.WaitingForTargetEntity && _boundaries.Count == 0)
            prompt = "2/2: No visible boundaries remain. E selects new boundaries; A uses all visible boundaries; Enter finishes.";
        return new CommandPromptState(
            "EXTEND", prompt, CommandInputKind.SelectionOrOption, options,
            acceptsEmptyEnter: true,
            placeholder: State == ExtendToolState.WaitingForBoundaryEntity
                ? "Click a boundary or press Enter for All"
                : "Click near an endpoint; dashed green shows the addition",
            feedback: _feedback);
    }

    public ToolResult HandleCommandInput(CommandInputSubmission input, ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        RefreshBoundaries(context);
        ToolResult result;
        if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Undo", StringComparison.OrdinalIgnoreCase))
        {
            result = UndoLastExtend(context);
        }
        else if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Edges", StringComparison.OrdinalIgnoreCase))
        {
            ClearBoundaries();
            State = ExtendToolState.WaitingForBoundaryEntity;
            context.CurrentBasePoint = null;
            result = ToolResult.Started("Choose a new boundary, or Enter for All. Completed extensions are kept; U undoes an extension.");
        }
        else if ((input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "All", StringComparison.OrdinalIgnoreCase)) ||
            (input.Kind == CommandInputSubmissionKind.Confirm && State == ExtendToolState.WaitingForBoundaryEntity))
        {
            result = UseAllBoundaries(context);
        }
        else if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            Reset(context);
            IsCommandFinished = true;
            result = ToolResult.Completed("Extend command finished.");
        }
        else
        {
            ClearPreview();
            result = ToolResult.None("Click a boundary or an endpoint on the canvas; use the displayed options. EXTEND does not accept measurements.");
        }
        _feedback = result.Message;
        return result;
    }

    public ToolResult OnPointerPressed(ToolContext context, PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);
        RefreshBoundaries(context);
        ClearPreview();
        ToolResult result;
        if (State == ExtendToolState.WaitingForBoundaryEntity || pointer.IsControlPressed)
        {
            result = AcceptBoundary(context, pointer);
        }
        else
        {
            CadEntity? target = FindTarget(context, pointer.ModelPoint);
            CadEntity? extended = TryExtend(context, target, pointer.ModelPoint);
            if (target is null || extended is null)
            {
                result = ToolResult.None(BuildFailureMessage(context, target, pointer.ModelPoint));
            }
            else
            {
                var command = new ModifyEntitiesCommand(new[] { target }, new[] { extended }, "Extend entity");
                context.Commands.Execute(context.Document, command);
                _extendCommands.Add(command);
                context.CurrentBasePoint = null;
                RefreshBoundaries(context);
                result = ToolResult.Completed("Entity extended. Click another endpoint, U undoes, E changes boundaries; Enter/right-click/Escape finishes.");
            }
        }
        _feedback = result.Message;
        return result;
    }

    public ToolResult OnPointerMoved(ToolContext context, PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);
        RefreshBoundaries(context);
        ClearPreview();
        if (State != ExtendToolState.WaitingForTargetEntity)
            return ToolResult.None();
        if (pointer.IsControlPressed)
        {
            _previewBoundaryCandidate = FindVisibleBoundary(context, pointer.ModelPoint);
            _feedback = _usesAllBoundaries
                ? "All mode already includes visible boundaries. E chooses specific boundaries."
                : "Ctrl-click to add a boundary; release Ctrl to preview the endpoint extension.";
            return ToolResult.Updated(_feedback);
        }

        CadEntity? target = FindTarget(context, pointer.ModelPoint);
        _previewEntity = TryExtend(context, target, pointer.ModelPoint);
        if (target is not null && _previewEntity is not null)
        {
            _previewTargetEntity = target;
            _highlightPreviewEntities = CreateExtendHighlightEntities(target, _previewEntity, context.GeometryTolerance);
            CadExtendService.TryGetPickedEndpoint(target, pointer.ModelPoint, out Point2D endpoint, context.GeometryTolerance);
            _previewEndpoint = endpoint;
        }
        _feedback = HasPreview
            ? "Click near this endpoint to add the dashed green portion. Solid green is the existing geometry; red marks the boundaries."
            : BuildFailureMessage(context, target, pointer.ModelPoint);
        return HasPreview ? ToolResult.Updated(_feedback) : ToolResult.None(_feedback);
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

    public IReadOnlyList<CadEntity> GetPreviewEntities() =>
        _previewEntity is null ? Array.Empty<CadEntity>() : new[] { _previewEntity };

    public IReadOnlyList<CadEntity> GetHighlightedPreviewEntities() => _highlightPreviewEntities.ToArray();

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        RefreshBoundaries(context);
        var overlays = new List<ToolPreviewEntityOverlay>();
        CadEntity[] boundaries = _boundaries.Where(edge => !HasPreview || edge.Id != _previewTargetEntity?.Id).ToArray();
        if (boundaries.Length > 0)
            overlays.Add(new ToolPreviewEntityOverlay(boundaries, ToolPreviewHighlightKind.Emphasis));
        if (_previewBoundaryCandidate is not null)
            overlays.Add(new ToolPreviewEntityOverlay(new[] { _previewBoundaryCandidate }, ToolPreviewHighlightKind.Emphasis));
        return new ToolPreviewDescriptor(
            entities: HasPreview && _previewTargetEntity is not null ? new[] { _previewTargetEntity } : Array.Empty<CadEntity>(),
            highlightedEntities: _highlightPreviewEntities,
            highlightedEntityKind: ToolPreviewHighlightKind.ExtendAddition,
            markers: _previewEndpoint is { } endpoint
                ? new[] { new ToolPreviewMarker(endpoint, ToolPreviewMarkerKind.Hot, ToolPreviewMarkerShape.Circle) }
                : Array.Empty<ToolPreviewMarker>(),
            entityOverlays: overlays,
            entityHighlightKind: ToolPreviewHighlightKind.Addition,
            replacedEntityIds: HasPreview && _previewTargetEntity is not null ? new[] { _previewTargetEntity.Id } : Array.Empty<EntityId>());
    }

    private CadEntity? TryExtend(ToolContext context, CadEntity? target, Point2D point)
    {
        if (target is null || !IsSupportedTargetEntity(target) ||
            !context.Document.IsEntitySelectable(target) ||
            (!_usesAllBoundaries && _selectedBoundaryIds.Contains(target.Id)))
            return null;
        return CadExtendService.ExtendToBoundaries(target, _boundaries, point, context.GeometryTolerance);
    }

    private string BuildFailureMessage(ToolContext context, CadEntity? target, Point2D point)
    {
        if (_boundaries.Count == 0)
            return "No visible boundaries remain. Use E to choose new boundaries or A for All.";
        if (target is null)
            return "Hover or click near the endpoint of an open curve to extend it to a red boundary.";
        if (!_usesAllBoundaries && _selectedBoundaryIds.Contains(target.Id))
            return "Target entity must be different from the boundary entity.";
        if (!IsSupportedTargetEntity(target))
            return "Extend supports lines, arcs, elliptical arcs and open polylines as targets. Closed curves cannot be extended.";
        if (!context.Document.IsEntitySelectable(target))
            return "Target entity is not editable. Unlock its layer or choose another endpoint.";
        if (!CadExtendService.TryGetPickedEndpoint(target, point, out Point2D endpoint, context.GeometryTolerance))
            return "The endpoint side is ambiguous or the curve is degenerate. Click closer to the endpoint to EXTEND, away from the middle.";
        if (target is PolylineEntity polyline)
        {
            bool start = context.GeometryTolerance.ArePointsEqual(endpoint, polyline.Vertices[0]);
            int segment = start ? 0 : polyline.SegmentCount - 1;
            if (Math.Abs(polyline.SegmentBulges[segment]) > context.GeometryTolerance.Distance)
                return "A curved polyline endpoint cannot be extended yet. Pick a straight endpoint or a separate arc.";
            Point2D adjacent = start ? polyline.Vertices[1] : polyline.Vertices[^2];
            if (endpoint.DistanceTo(adjacent) <= context.GeometryTolerance.Distance)
                return "The endpoint segment has zero length. Choose a non-degenerate endpoint.";
        }
        CadEntity[] edges = _boundaries.Where(edge => edge.Id != target.Id).ToArray();
        if (edges.Length == 0)
            return "No boundary is available for this target. Ctrl-click a boundary or use E to choose again.";
        if (edges.Any(edge => edge.DistanceTo(endpoint) <= context.GeometryTolerance.Distance))
            return "This endpoint already meets a boundary. Pick the opposite endpoint or use E to change boundaries.";
        return edges.Length == 1
            ? EditingStatusMessageBuilder.BuildExtendFailureMessage(target, edges[0], point, context.GeometryTolerance)
            : "No selected boundary is reachable beyond this endpoint. Pick the opposite endpoint or use E to change boundaries.";
    }

    private ToolResult AcceptBoundary(ToolContext context, PointerInfo pointer)
    {
        if (State == ExtendToolState.WaitingForTargetEntity && _usesAllBoundaries)
            return ToolResult.None("All mode already includes visible boundaries. Use E to choose specific boundaries.");
        CadEntity? boundary = FindVisibleBoundary(context, pointer.ModelPoint);
        if (boundary is null)
            return ToolResult.None("Select a visible line, circle, arc, ellipse, elliptical arc or polyline boundary.");
        if (!_selectedBoundaryIds.Add(boundary.Id))
            return ToolResult.None("Boundary is already selected.");
        _boundaries.Add(boundary);
        State = ExtendToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = null;
        return ToolResult.Started($"Boundary selected ({_boundaries.Count}). Click near the endpoint to EXTEND; dashed green previews the addition. Ctrl-click adds a boundary.");
    }

    private ToolResult UseAllBoundaries(ToolContext context)
    {
        CadEntity[] boundaries = context.Document.GetVisibleEntities().Where(IsSupportedBoundaryEntity).ToArray();
        if (boundaries.Length == 0)
            return ToolResult.None("No visible supported boundaries found.");
        ClearBoundaries();
        _usesAllBoundaries = true;
        _boundaries.AddRange(boundaries);
        State = ExtendToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = null;
        return ToolResult.Started($"Using all visible boundaries ({boundaries.Length}). Click near an endpoint; the nearest reachable boundary stops the extension.");
    }

    private ToolResult UndoLastExtend(ToolContext context)
    {
        ICadCommand? nextUndo = context.Commands.History.NextUndoCommand;
        if (nextUndo is null || !_extendCommands.Contains(nextUndo))
            return ToolResult.None("No extension from this command is next to undo. U will not undo other actions; use global Undo if needed.");
        context.Commands.History.Undo(context.Document);
        ClearPreview();
        RefreshBoundaries(context);
        context.CurrentBasePoint = null;
        return ToolResult.Updated("Last extension undone. Click another endpoint, or E to change boundaries.");
    }

    private static IReadOnlyList<CadEntity> CreateExtendHighlightEntities(
        CadEntity source,
        CadEntity extended,
        GeometryTolerance tolerance)
    {
        return (source, extended) switch
        {
            (LineEntity sourceLine, LineEntity extendedLine) =>
                CreateLineExtendHighlightEntities(sourceLine, extendedLine, tolerance),

            (ArcEntity sourceArc, ArcEntity extendedArc) =>
                CreateArcExtendHighlightEntities(sourceArc, extendedArc, tolerance),

            (EllipticalArcEntity sourceArc, EllipticalArcEntity extendedArc) =>
                CreateEllipticalArcExtendHighlightEntities(sourceArc, extendedArc, tolerance),

            (PolylineEntity sourcePolyline, PolylineEntity extendedPolyline) =>
                CreatePolylineExtendHighlightEntities(sourcePolyline, extendedPolyline, tolerance),

            _ => Array.Empty<CadEntity>()
        };
    }

    private static IReadOnlyList<CadEntity> CreateLineExtendHighlightEntities(
        LineEntity sourceLine,
        LineEntity extendedLine,
        GeometryTolerance tolerance)
    {
        if (!tolerance.ArePointsEqual(sourceLine.Start, extendedLine.Start))
        {
            return CreateHighlightLine(
                sourceLine,
                extendedLine.Start,
                sourceLine.Start,
                tolerance);
        }

        if (!tolerance.ArePointsEqual(sourceLine.End, extendedLine.End))
        {
            return CreateHighlightLine(
                sourceLine,
                sourceLine.End,
                extendedLine.End,
                tolerance);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreateArcExtendHighlightEntities(
        ArcEntity sourceArc,
        ArcEntity extendedArc,
        GeometryTolerance tolerance)
    {
        if (!tolerance.ArePointsEqual(
                sourceArc.Geometry.StartPoint,
                extendedArc.Geometry.StartPoint))
        {
            return CreateHighlightArc(
                sourceArc,
                extendedArc.StartAngle,
                sourceArc.StartAngle);
        }

        if (!tolerance.ArePointsEqual(
                sourceArc.Geometry.EndPoint,
                extendedArc.Geometry.EndPoint))
        {
            return CreateHighlightArc(
                sourceArc,
                sourceArc.EndAngle,
                extendedArc.EndAngle);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreateEllipticalArcExtendHighlightEntities(
        EllipticalArcEntity sourceArc,
        EllipticalArcEntity extendedArc,
        GeometryTolerance tolerance)
    {
        if (!tolerance.ArePointsEqual(sourceArc.StartPoint, extendedArc.StartPoint))
        {
            return CreateHighlightEllipticalArc(
                sourceArc,
                extendedArc.StartParameterRadians,
                sourceArc.StartParameterRadians);
        }

        if (!tolerance.ArePointsEqual(sourceArc.EndPoint, extendedArc.EndPoint))
        {
            return CreateHighlightEllipticalArc(
                sourceArc,
                sourceArc.EndParameterRadians,
                extendedArc.EndParameterRadians);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreatePolylineExtendHighlightEntities(
        PolylineEntity sourcePolyline,
        PolylineEntity extendedPolyline,
        GeometryTolerance tolerance)
    {
        if (sourcePolyline.IsClosed ||
            extendedPolyline.IsClosed ||
            sourcePolyline.Vertices.Count < 2 ||
            extendedPolyline.Vertices.Count < 2)
        {
            return Array.Empty<CadEntity>();
        }

        if (!tolerance.ArePointsEqual(
                sourcePolyline.Vertices[0],
                extendedPolyline.Vertices[0]))
        {
            return CreateHighlightLine(
                sourcePolyline,
                extendedPolyline.Vertices[0],
                sourcePolyline.Vertices[0],
                tolerance);
        }

        if (!tolerance.ArePointsEqual(
                sourcePolyline.Vertices[^1],
                extendedPolyline.Vertices[^1]))
        {
            return CreateHighlightLine(
                sourcePolyline,
                sourcePolyline.Vertices[^1],
                extendedPolyline.Vertices[^1],
                tolerance);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreateHighlightArc(
        ArcEntity source,
        Angle startAngle,
        Angle endAngle)
    {
        return new[]
        {
            new ArcEntity(
                source.Center,
                source.Radius,
                startAngle,
                endAngle,
                source.IsCounterClockwise,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
        };
    }

    private static IReadOnlyList<CadEntity> CreateHighlightEllipticalArc(
        EllipticalArcEntity source,
        double startParameterRadians,
        double endParameterRadians)
    {
        return new[]
        {
            new EllipticalArcEntity(
                source.Center,
                source.MajorAxis,
                source.MinorRadius,
                startParameterRadians,
                endParameterRadians,
                source.IsCounterClockwise,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
        };
    }

    private static IReadOnlyList<CadEntity> CreateHighlightLine(
        CadEntity source,
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


    private static CadEntity? FindVisibleBoundary(ToolContext context, Point2D point)
    {
        var area = new BoundingBox2D(
            point.X - context.Selection.Tolerance, point.Y - context.Selection.Tolerance,
            point.X + context.Selection.Tolerance, point.Y + context.Selection.Tolerance);
        return context.Document.GetVisibleEntities(area)
            .Where(IsSupportedBoundaryEntity)
            .Select(entity => new { Entity = entity, Distance = entity.DistanceTo(point) })
            .Where(hit => hit.Distance <= context.Selection.Tolerance)
            .OrderBy(hit => hit.Distance)
            .ThenByDescending(hit => hit.Entity.DrawOrder)
            .Select(hit => hit.Entity)
            .FirstOrDefault();
    }

    private CadEntity? FindTarget(ToolContext context, Point2D point)
    {
        IReadOnlyList<EntityId> hits = context.Selection.Service.SelectAllByPoint(
            context.Document, point, context.Selection.Tolerance);
        CadEntity? fallback = null;
        foreach (EntityId id in hits)
        {
            if (!_usesAllBoundaries && _selectedBoundaryIds.Contains(id))
                continue;
            CadEntity entity = context.Document.Entities.GetRequired(id);
            fallback ??= entity;
            if (IsSupportedTargetEntity(entity))
                return entity;
        }
        if (fallback is not null)
            return fallback;
        // Locked curves may explain a failed pick, but never enter the mutation path.
        var area = new BoundingBox2D(
            point.X - context.Selection.Tolerance, point.Y - context.Selection.Tolerance,
            point.X + context.Selection.Tolerance, point.Y + context.Selection.Tolerance);
        CadEntity? visibleTarget = context.Document.GetVisibleEntities(area)
            .Where(entity => IsSupportedTargetEntity(entity) &&
                (_usesAllBoundaries || !_selectedBoundaryIds.Contains(entity.Id)))
            .Select(entity => new { Entity = entity, Distance = entity.DistanceTo(point) })
            .Where(hit => hit.Distance <= context.Selection.Tolerance)
            .OrderBy(hit => hit.Distance)
            .ThenByDescending(hit => hit.Entity.DrawOrder)
            .Select(hit => hit.Entity)
            .FirstOrDefault();
        return visibleTarget ?? (hits.Count > 0 ? context.Document.Entities.GetRequired(hits[0]) : null);
    }

    private static bool IsSupportedBoundaryEntity(CadEntity entity) =>
        entity is LineEntity or CircleEntity or ArcEntity or EllipseEntity or EllipticalArcEntity or PolylineEntity;

    private static bool IsSupportedTargetEntity(CadEntity entity) =>
        entity is LineEntity or ArcEntity or EllipticalArcEntity or PolylineEntity { IsClosed: false };

    private void RefreshBoundaries(ToolContext context)
    {
        List<CadEntity> current = (_usesAllBoundaries
            ? context.Document.GetVisibleEntities().Where(IsSupportedBoundaryEntity)
            : _selectedBoundaryIds
                .Select(id => context.Document.Entities.TryGet(id, out CadEntity? entity) ? entity : null)
                .OfType<CadEntity>()
                .Where(entity => IsSupportedBoundaryEntity(entity) && context.Document.IsEntityVisible(entity))).ToList();
        if (!_boundaries.SequenceEqual(current) ||
            (_previewTargetEntity is not null &&
                (!context.Document.Entities.TryGet(_previewTargetEntity.Id, out CadEntity? target) ||
                !ReferenceEquals(target, _previewTargetEntity) || !context.Document.IsEntitySelectable(target))))
        {
            if (HasPreview)
                _feedback = current.Count == 0
                    ? "No visible boundaries remain. Use E to choose new boundaries or A for All."
                    : "Drawing geometry changed. Hover near an endpoint again to update the extension preview.";
            ClearPreview();
        }
        if (_previewBoundaryCandidate is not null &&
            (!context.Document.Entities.TryGet(_previewBoundaryCandidate.Id, out CadEntity? candidate) ||
            !ReferenceEquals(candidate, _previewBoundaryCandidate) || !context.Document.IsEntityVisible(candidate)))
            _previewBoundaryCandidate = null;
        _boundaries.Clear();
        _boundaries.AddRange(current);
    }

    private void ClearPreview()
    {
        _previewTargetEntity = null;
        _previewBoundaryCandidate = null;
        _previewEndpoint = null;
        _previewEntity = null;
        _highlightPreviewEntities = Array.Empty<CadEntity>();
    }

    private void ClearBoundaries()
    {
        _selectedBoundaryIds.Clear();
        _boundaries.Clear();
        _usesAllBoundaries = false;
        ClearPreview();
    }

    private void Reset(ToolContext context)
    {
        ClearBoundaries();
        _extendCommands.Clear();
        _feedback = null;
        IsCommandFinished = false;
        State = ExtendToolState.WaitingForBoundaryEntity;
        context.CurrentBasePoint = null;
    }
}
