using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Breaks a line entity into two line entities at a picked point.
/// </summary>
public sealed class BreakAtPointTool : ICadTool
{
    private EntityId? _targetEntityId;
    private LineEntity? _targetLine;
    private Point2D? _currentBreakPoint;
    private IReadOnlyList<LineEntity> _previewSegments = Array.Empty<LineEntity>();

    public string Name => "Break Point";

    public BreakAtPointToolState State { get; private set; } =
        BreakAtPointToolState.WaitingForTargetEntity;

    public EntityId? TargetEntityId => _targetEntityId;

    public Point2D? CurrentBreakPoint => _currentBreakPoint;

    public bool HasPreview =>
        State == BreakAtPointToolState.WaitingForBreakPoint &&
        _previewSegments.Count > 0;

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
            _targetLine is null)
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
            : ToolResult.None("Break point must be inside the target line.");
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
            return ToolResult.None("Select a line to break.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (entity is not LineEntity line)
        {
            return ToolResult.None("Break Point currently supports line entities only.");
        }

        if (!context.Document.IsEntitySelectable(line))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        _targetEntityId = line.Id;
        _targetLine = line;
        State = BreakAtPointToolState.WaitingForBreakPoint;

        Point2D basePoint = line.GetClosestPoint(pointer.ModelPoint);
        context.CurrentBasePoint = basePoint;

        return ToolResult.Started(
            "Specify break point on line.");
    }

    private ToolResult AcceptBreakPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_targetLine is null)
        {
            throw new InvalidOperationException(
                "Cannot accept break point before selecting a target line.");
        }

        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        IReadOnlyList<LineEntity> segments = LineBreakService.BreakAtPoint(
            _targetLine,
            point,
            context.GeometryTolerance);

        if (segments.Count != 2)
        {
            return ToolResult.None(
                "Break point must be inside the target line and not too close to an endpoint.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { _targetLine },
                segments,
                "Break line at point"));

        Reset(context);

        return ToolResult.Completed("Line broken at point.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_targetLine is null)
        {
            _currentBreakPoint = null;
            _previewSegments = Array.Empty<LineEntity>();
            return;
        }

        double parameter = LineParameterService.GetParameter(
            _targetLine.Geometry,
            point,
            context.GeometryTolerance);

        _currentBreakPoint = LineParameterService.PointAt(
            _targetLine.Geometry,
            parameter);

        _previewSegments = LineBreakService.BreakAtPoint(
            _targetLine,
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
        _targetLine = null;
        _currentBreakPoint = null;
        _previewSegments = Array.Empty<LineEntity>();
        State = BreakAtPointToolState.WaitingForTargetEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
