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
/// Breaks a supported entity at a picked point.
/// A single editable supported entity selected before the command starts is used as the target;
/// Enter/right-click at the target prompt ends the command.
/// </summary>
public sealed class BreakAtPointTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, IToolPreviewDescriptorProvider, ISnapModeProvider
{
    private EntityId? _targetEntityId;
    private CadEntity? _targetEntity;
    private Point2D? _currentBreakPoint;
    private IReadOnlyList<CadEntity> _previewSegments = Array.Empty<CadEntity>();
    private bool _isInitialized;

    public string Name => "Break Point";

    public bool IsCommandFinished { get; private set; }

    public BreakAtPointToolState State { get; private set; } =
        BreakAtPointToolState.WaitingForTargetEntity;

    public EntityId? TargetEntityId => _targetEntityId;

    public Point2D? CurrentBreakPoint => _currentBreakPoint;

    public bool HasPreview =>
        State == BreakAtPointToolState.WaitingForBreakPoint &&
        _previewSegments.Count > 0;


    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialTarget(context);

        return State == BreakAtPointToolState.WaitingForTargetEntity
            ? SnapKind.EntityOnly
            : context.EnabledSnaps;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialTarget(context);

        return State switch
        {
            BreakAtPointToolState.WaitingForTargetEntity => new CommandPromptState(
                "BREAKPOINT",
                "Select entity or press Enter to finish",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Click a line, arc, elliptical arc, polyline or open spline; Enter finishes"),

            BreakAtPointToolState.WaitingForBreakPoint => new CommandPromptState(
                "BREAKPOINT",
                "Specify break point",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

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

        if (State == BreakAtPointToolState.WaitingForTargetEntity)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                Reset(context);
                IsCommandFinished = true;
                return ToolResult.Completed("Break Point command finished.");
            }

            return ToolResult.None("Select an entity to break from the drawing canvas.");
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "BREAKPOINT expects a point input.");
        }

        return AcceptBreakPoint(context, input.Point.Value);
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
            BreakAtPointToolState.WaitingForTargetEntity =>
                AcceptTargetEntity(context, pointer),

            BreakAtPointToolState.WaitingForBreakPoint =>
                AcceptBreakPoint(context, pointer),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != BreakAtPointToolState.WaitingForBreakPoint ||
            _targetEntity is null)
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
            ? ToolResult.Updated("Break point preview updated.")
            : ToolResult.None(
                EditingStatusMessageBuilder.BuildBreakAtPointFailureMessage(
                    _targetEntity,
                    point,
                    context.GeometryTolerance));
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Break Point command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Break Point tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetPreviewEntities();
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewSegments
            .Cast<CadEntity>()
            .ToList();
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ToolPreviewDescriptor(
            entities: GetPreviewEntities(),
            markers: GetBreakPointMarkers(),
            entityOverlays: GetSelectedTargetOverlays());
    }

    private IReadOnlyList<ToolPreviewMarker> GetBreakPointMarkers()
    {
        if (_currentBreakPoint is null || _previewSegments.Count == 0)
        {
            return Array.Empty<ToolPreviewMarker>();
        }

        return new[]
        {
            new ToolPreviewMarker(
                _currentBreakPoint.Value,
                ToolPreviewMarkerKind.Hot,
                ToolPreviewMarkerShape.Circle)
        };
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
            return ToolResult.None("Select an entity to break at point.");
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
        if (entity is CircleEntity)
        {
            return "Break Point is not applicable to circles. Use Break Segment with two points instead.";
        }

        if (entity is EllipseEntity)
        {
            return "Break Point is not applicable to full ellipses. Use Break Segment with two points instead.";
        }

        if (entity is not LineEntity and not ArcEntity and not EllipticalArcEntity and not PolylineEntity and not BezierSplineEntity)
        {
            return "Break Point supports lines, arcs, elliptical arcs, polylines and open splines only.";
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
        State = BreakAtPointToolState.WaitingForBreakPoint;
        context.CurrentBasePoint = basePoint;

        return ToolResult.Started(
            "Specify break point on entity. The point will be projected to the native curve.");
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

        if (State != BreakAtPointToolState.WaitingForTargetEntity ||
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
    /// change it before the break point is given; breaking the captured copy would then fail
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

    private ToolResult AcceptBreakPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        return AcceptBreakPoint(context, point);
    }

    private ToolResult AcceptBreakPoint(
        ToolContext context,
        Point2D point)
    {
        if (_targetEntity is null)
        {
            throw new InvalidOperationException(
                "Cannot accept break point before selecting a target entity.");
        }

        if (!IsTargetCurrent(context))
        {
            return ResetForChangedTarget(context);
        }

        IReadOnlyList<CadEntity> segments = CadBreakService.BreakAtPoint(
            _targetEntity,
            point,
            context.GeometryTolerance);

        if (segments.Count == 0)
        {
            return ToolResult.None(
                EditingStatusMessageBuilder.BuildBreakAtPointFailureMessage(
                    _targetEntity,
                    point,
                    context.GeometryTolerance));
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { _targetEntity! },
                segments,
                "Break entity at point"));

        context.Selection.Set.Deselect(_targetEntity.Id);
        Reset(context);

        return ToolResult.Completed("Entity broken at point.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_targetEntity is null)
        {
            _currentBreakPoint = null;
            _previewSegments = Array.Empty<CadEntity>();
            return;
        }

        _currentBreakPoint = _targetEntity.GetClosestPoint(point);

        _previewSegments = CadBreakService.BreakAtPoint(
            _targetEntity,
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
        _currentBreakPoint = null;
        _previewSegments = Array.Empty<CadEntity>();
        IsCommandFinished = false;
        State = BreakAtPointToolState.WaitingForTargetEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
