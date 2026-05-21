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
/// </summary>
public sealed class BreakAtPointTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, ISnapModeProvider
{
    private EntityId? _targetEntityId;
    private CadEntity? _targetEntity;
    private Point2D? _currentBreakPoint;
    private IReadOnlyList<CadEntity> _previewSegments = Array.Empty<CadEntity>();

    public string Name => "Break Point";

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

        return State == BreakAtPointToolState.WaitingForTargetEntity
            ? SnapKind.EntityOnly
            : context.EnabledSnaps;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            BreakAtPointToolState.WaitingForTargetEntity => new CommandPromptState(
                "BREAKPOINT",
                "Select entity",
                CommandInputKind.Selection,
                placeholder: "Click a line, arc, elliptical arc, polyline or open spline"),

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

        if (State == BreakAtPointToolState.WaitingForTargetEntity)
        {
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

        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        UpdatePreview(
            context,
            point);

        return HasPreview
            ? ToolResult.Updated("Break point preview updated.")
            : ToolResult.None("Break point must be inside the target entity.");
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

        if (entity is CircleEntity)
        {
            return ToolResult.None("Break Point is not applicable to circles. Use Break Segment with two points instead.");
        }

        if (entity is EllipseEntity)
        {
            return ToolResult.None("Break Point is not applicable to full ellipses. Use Break Segment with two points instead.");
        }

        if (entity is not LineEntity and not ArcEntity and not EllipticalArcEntity and not PolylineEntity and not BezierSplineEntity)
        {
            return ToolResult.None("Break Point supports lines, arcs, elliptical arcs, polylines and open splines only.");
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        _targetEntityId = entity.Id;
        _targetEntity = entity;
        State = BreakAtPointToolState.WaitingForBreakPoint;

        Point2D basePoint = entity.GetClosestPoint(pointer.ModelPoint);
        context.CurrentBasePoint = basePoint;

        return ToolResult.Started(
            "Specify break point on entity. The point will be projected to the native curve.");
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

        IReadOnlyList<CadEntity> segments = CadBreakService.BreakAtPoint(
            _targetEntity,
            point,
            context.GeometryTolerance);

        if (segments.Count == 0)
        {
            return ToolResult.None(
                "Break point must be inside the target entity and not too close to an endpoint.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { _targetEntity! },
                segments,
                "Break entity at point"));

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
        State = BreakAtPointToolState.WaitingForTargetEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
