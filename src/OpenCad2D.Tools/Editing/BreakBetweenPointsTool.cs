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
/// Breaks a supported entity by removing the segment between two picked break points.
/// Break points are projected onto the target. A single editable supported entity selected before
/// the command starts is used as the target; Enter/right-click at the target prompt ends the command.
/// </summary>
public sealed class BreakBetweenPointsTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, IToolPreviewDescriptorProvider, ISnapModeProvider
{
    private EntityId? _targetEntityId;
    private CadEntity? _targetEntity;
    private Point2D? _firstBreakPoint;
    private Point2D? _currentSecondBreakPoint;
    private IReadOnlyList<CadEntity> _previewSegments = Array.Empty<CadEntity>();
    private IReadOnlyList<CadEntity> _removedPreviewSegments = Array.Empty<CadEntity>();
    private bool _isInitialized;

    public string Name => "Break Segment";

    public bool IsCommandFinished { get; private set; }

    public BreakBetweenPointsToolState State { get; private set; } =
        BreakBetweenPointsToolState.WaitingForTargetEntity;

    public EntityId? TargetEntityId => _targetEntityId;

    public Point2D? FirstBreakPoint => _firstBreakPoint;

    public Point2D? CurrentSecondBreakPoint => _currentSecondBreakPoint;

    public bool HasPreview =>
        State == BreakBetweenPointsToolState.WaitingForSecondBreakPoint &&
        (_previewSegments.Count > 0 || _removedPreviewSegments.Count > 0);

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialTarget(context);

        return State == BreakBetweenPointsToolState.WaitingForTargetEntity
            ? SnapKind.EntityOnly
            : context.EnabledSnaps;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialTarget(context);

        return State switch
        {
            BreakBetweenPointsToolState.WaitingForTargetEntity => new CommandPromptState(
                "BREAK",
                "Select entity or press Enter to finish",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Click a line, arc, circle, ellipse, elliptical arc, polyline or open spline; Enter finishes"),

            BreakBetweenPointsToolState.WaitingForFirstBreakPoint => new CommandPromptState(
                "BREAK",
                "Specify first break point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            BreakBetweenPointsToolState.WaitingForSecondBreakPoint => new CommandPromptState(
                "BREAK",
                "Specify second break point",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   @100<45   |   distance"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialTarget(context);

        if (State == BreakBetweenPointsToolState.WaitingForTargetEntity &&
            input.Kind == CommandInputSubmissionKind.Confirm)
        {
            Reset(context);
            IsCommandFinished = true;
            return ToolResult.Completed("Break Segment command finished.");
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return State == BreakBetweenPointsToolState.WaitingForTargetEntity
                ? ToolResult.None("Select an entity to break from the drawing canvas.")
                : ToolResult.None(input.ErrorMessage ?? "BREAK expects a point input.");
        }

        return State switch
        {
            BreakBetweenPointsToolState.WaitingForFirstBreakPoint => AcceptFirstBreakPoint(context, input.Point.Value),
            BreakBetweenPointsToolState.WaitingForSecondBreakPoint => AcceptSecondBreakPoint(context, input.Point.Value),
            BreakBetweenPointsToolState.WaitingForTargetEntity => ToolResult.None("Select an entity to break from the drawing canvas."),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        EnsureInitialTarget(context);

        return State switch
        {
            BreakBetweenPointsToolState.WaitingForTargetEntity =>
                AcceptTargetEntity(context, pointer),

            BreakBetweenPointsToolState.WaitingForFirstBreakPoint =>
                AcceptFirstBreakPoint(context, pointer),

            BreakBetweenPointsToolState.WaitingForSecondBreakPoint =>
                AcceptSecondBreakPoint(context, pointer),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != BreakBetweenPointsToolState.WaitingForSecondBreakPoint ||
            _targetEntity is null ||
            _firstBreakPoint is null)
        {
            return ToolResult.None();
        }

        if (!IsTargetCurrent(context))
        {
            return ResetForChangedTarget(context);
        }

        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        UpdatePreview(
            context,
            point);

        return HasPreview
            ? ToolResult.Updated("Break Segment preview updated. Dashed portion will be removed.")
            : ToolResult.None(
                EditingStatusMessageBuilder.BuildBreakBetweenPointsFailureMessage(
                    _targetEntity,
                    _firstBreakPoint.Value,
                    point,
                    context.GeometryTolerance));
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Break Segment command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Break Segment tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetPreviewEntities();
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewSegments;
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ToolPreviewDescriptor(
            entities: _previewSegments,
            highlightedEntities: _removedPreviewSegments,
            highlightedEntityKind: ToolPreviewHighlightKind.Removal,
            markers: GetBreakPointMarkers(),
            entityOverlays: GetSelectedTargetOverlays());
    }

    private IReadOnlyList<ToolPreviewMarker> GetBreakPointMarkers()
    {
        var markers = new List<ToolPreviewMarker>();

        if (_firstBreakPoint is not null)
        {
            markers.Add(new ToolPreviewMarker(
                _firstBreakPoint.Value,
                ToolPreviewMarkerKind.Primary,
                ToolPreviewMarkerShape.Circle));
        }

        if (_currentSecondBreakPoint is not null && _removedPreviewSegments.Count > 0)
        {
            markers.Add(new ToolPreviewMarker(
                _currentSecondBreakPoint.Value,
                ToolPreviewMarkerKind.Hot,
                ToolPreviewMarkerShape.Circle));
        }

        return markers;
    }

    private IReadOnlyList<ToolPreviewEntityOverlay> GetSelectedTargetOverlays()
    {
        if (_targetEntity is null)
        {
            return Array.Empty<ToolPreviewEntityOverlay>();
        }

        return new[]
        {
            new ToolPreviewEntityOverlay(
                new[] { _targetEntity },
                ToolPreviewHighlightKind.Emphasis)
        };
    }

    private ToolResult AcceptTargetEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return ToolResult.None("Select an entity to break.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        string? error = ValidateTarget(context, entity);
        if (error is not null)
        {
            return ToolResult.None(error);
        }

        return AdoptTarget(context, entity, entity.GetClosestPoint(pointer.ModelPoint));
    }

    private static string? ValidateTarget(
        ToolContext context,
        CadEntity entity)
    {
        if (entity is not LineEntity and not ArcEntity and not CircleEntity and not EllipseEntity and not EllipticalArcEntity and not PolylineEntity and not BezierSplineEntity)
        {
            return "Break Segment supports lines, arcs, circles, ellipses, elliptical arcs, polylines and open splines only.";
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return "Target entity is not editable.";
        }

        return null;
    }

    private ToolResult AdoptTarget(
        ToolContext context,
        CadEntity entity,
        Point2D? basePoint)
    {
        _targetEntityId = entity.Id;
        _targetEntity = entity;
        State = BreakBetweenPointsToolState.WaitingForFirstBreakPoint;
        context.CurrentBasePoint = basePoint;

        return ToolResult.Started("Specify first break point on entity. For closed curves, point order defines which side is removed.");
    }

    /// <summary>
    /// Uses a single editable supported entity selected before the command as the target,
    /// matching the selection-first behavior of the other modify tools.
    /// </summary>
    private void EnsureInitialTarget(ToolContext context)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (State != BreakBetweenPointsToolState.WaitingForTargetEntity ||
            context.Selection.SelectedIds.Count != 1)
        {
            return;
        }

        EntityId id = context.Selection.SelectedIds.First();
        if (context.Document.Entities.TryGet(id, out CadEntity? entity) &&
            entity is not null &&
            ValidateTarget(context, entity) is null)
        {
            AdoptTarget(context, entity, basePoint: null);
        }
    }

    /// <summary>
    /// The target is captured when it is picked. Undo, delete, a grip edit or a layer lock can
    /// change it before the break points are given; breaking the captured copy would then fail
    /// half-way or bring back geometry that no longer exists.
    /// </summary>
    private bool IsTargetCurrent(ToolContext context)
    {
        return _targetEntityId is { } id &&
               context.Document.Entities.TryGet(id, out CadEntity? current) &&
               current is not null &&
               ReferenceEquals(current, _targetEntity) &&
               context.Document.IsEntitySelectable(current);
    }

    private ToolResult ResetForChangedTarget(ToolContext context)
    {
        Reset(context);

        return ToolResult.None(
            "The selected entity was changed, deleted or is no longer editable. Select the entity to break again.");
    }

    private ToolResult AcceptFirstBreakPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        return AcceptFirstBreakPoint(context, point);
    }

    private ToolResult AcceptFirstBreakPoint(
        ToolContext context,
        Point2D point)
    {
        if (_targetEntity is null)
        {
            throw new InvalidOperationException(
                "Cannot accept first break point before selecting a target entity.");
        }

        if (!IsTargetCurrent(context))
        {
            return ResetForChangedTarget(context);
        }

        // Like Break Point and the second break point, the first point is projected onto the
        // native curve (the former "must be on the entity" check measured the projection
        // against its own entity and could never fail).
        _firstBreakPoint = _targetEntity.GetClosestPoint(point);

        _currentSecondBreakPoint = null;
        _previewSegments = Array.Empty<CadEntity>();
        _removedPreviewSegments = Array.Empty<CadEntity>();
        State = BreakBetweenPointsToolState.WaitingForSecondBreakPoint;
        context.CurrentBasePoint = _firstBreakPoint;

        return ToolResult.Updated("Specify second break point. The dashed preview shows the segment that will be removed.");
    }

    private ToolResult AcceptSecondBreakPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        return AcceptSecondBreakPoint(context, point);
    }

    private ToolResult AcceptSecondBreakPoint(
        ToolContext context,
        Point2D point)
    {
        if (_targetEntity is null ||
            _firstBreakPoint is null)
        {
            throw new InvalidOperationException(
                "Cannot accept second break point before selecting a target entity and first break point.");
        }

        if (!IsTargetCurrent(context))
        {
            return ResetForChangedTarget(context);
        }

        IReadOnlyList<CadEntity> segments = CadBreakService.BreakBetweenPoints(
            _targetEntity,
            _firstBreakPoint.Value,
            point,
            context.GeometryTolerance);

        if (segments.Count == 0)
        {
            return ToolResult.None(
                EditingStatusMessageBuilder.BuildBreakBetweenPointsFailureMessage(
                    _targetEntity,
                    _firstBreakPoint.Value,
                    point,
                    context.GeometryTolerance));
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { _targetEntity },
                segments,
                "Break entity between points"));

        context.Selection.Set.Deselect(_targetEntity.Id);
        Reset(context);

        return ToolResult.Completed("Entity segment removed.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_targetEntity is null ||
            _firstBreakPoint is null)
        {
            _currentSecondBreakPoint = null;
            _previewSegments = Array.Empty<CadEntity>();
            _removedPreviewSegments = Array.Empty<CadEntity>();
            return;
        }

        _currentSecondBreakPoint = _targetEntity.GetClosestPoint(point);

        _previewSegments = CadBreakService.BreakBetweenPoints(
            _targetEntity,
            _firstBreakPoint.Value,
            point,
            context.GeometryTolerance);

        _removedPreviewSegments = CadBreakService.GetRemovedSegmentBetweenPoints(
            _targetEntity,
            _firstBreakPoint.Value,
            point,
            context.GeometryTolerance);
    }

    private static Point2D ResolvePoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        if (context.EnabledSnaps == SnapKind.None ||
            Tolerance.IsZero(context.SnapTolerance))
        {
            return cursorPoint;
        }

        var request = new SnapRequest(
            context.Document,
            cursorPoint,
            context.SnapTolerance,
            context.EnabledSnaps,
            context.CurrentBasePoint,
            context.GridSettings);

        SnapCandidate? candidate = context.SnapService.Snap(request);

        return candidate?.Point ?? cursorPoint;
    }

    private void Reset(ToolContext? context = null)
    {
        _targetEntityId = null;
        _targetEntity = null;
        _firstBreakPoint = null;
        _currentSecondBreakPoint = null;
        _previewSegments = Array.Empty<CadEntity>();
        _removedPreviewSegments = Array.Empty<CadEntity>();
        IsCommandFinished = false;
        State = BreakBetweenPointsToolState.WaitingForTargetEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
