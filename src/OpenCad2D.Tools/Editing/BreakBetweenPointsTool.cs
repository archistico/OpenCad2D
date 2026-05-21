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
/// </summary>
public sealed class BreakBetweenPointsTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, IToolPreviewDescriptorProvider, ISnapModeProvider
{
    private EntityId? _targetEntityId;
    private CadEntity? _targetEntity;
    private Point2D? _firstBreakPoint;
    private Point2D? _currentSecondBreakPoint;
    private IReadOnlyList<CadEntity> _previewSegments = Array.Empty<CadEntity>();
    private IReadOnlyList<CadEntity> _removedPreviewSegments = Array.Empty<CadEntity>();

    public string Name => "Break Segment";

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

        return State == BreakBetweenPointsToolState.WaitingForTargetEntity
            ? SnapKind.EntityOnly
            : context.EnabledSnaps;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            BreakBetweenPointsToolState.WaitingForTargetEntity => new CommandPromptState(
                "BREAK",
                "Select entity",
                CommandInputKind.Selection,
                placeholder: "Click a line, arc, circle, ellipse, elliptical arc, polyline or open spline"),

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

        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        UpdatePreview(
            context,
            point);

        return HasPreview
            ? ToolResult.Updated("Break Segment preview updated. Dashed portion will be removed.")
            : ToolResult.None("Second break point must be different and on the target entity.");
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
            highlightedEntityKind: ToolPreviewHighlightKind.Removal);
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

        if (entity is not LineEntity and not ArcEntity and not CircleEntity and not EllipseEntity and not EllipticalArcEntity and not PolylineEntity and not BezierSplineEntity)
        {
            return ToolResult.None("Break Segment supports lines, arcs, circles, ellipses, elliptical arcs, polylines and open splines only.");
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        _targetEntityId = entity.Id;
        _targetEntity = entity;
        State = BreakBetweenPointsToolState.WaitingForFirstBreakPoint;

        Point2D basePoint = entity.GetClosestPoint(pointer.ModelPoint);
        context.CurrentBasePoint = basePoint;

        return ToolResult.Started("Specify first break point on entity. For closed curves, point order defines which side is removed.");
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

        Point2D projectedPoint = _targetEntity.GetClosestPoint(point);

        if (_targetEntity.DistanceTo(projectedPoint) > context.GeometryTolerance.Distance)
        {
            return ToolResult.None("First break point must be on the target entity.");
        }

        _firstBreakPoint = projectedPoint;

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
        State = BreakBetweenPointsToolState.WaitingForTargetEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
