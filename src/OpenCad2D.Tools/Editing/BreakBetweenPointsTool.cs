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
/// Breaks a line entity by removing the segment between two picked break points.
/// </summary>
public sealed class BreakBetweenPointsTool : ICadTool
{
    private EntityId? _targetEntityId;
    private LineEntity? _targetLine;
    private Point2D? _firstBreakPoint;
    private Point2D? _currentSecondBreakPoint;
    private IReadOnlyList<LineEntity> _previewSegments = Array.Empty<LineEntity>();

    public string Name => "Break Segment";

    public BreakBetweenPointsToolState State { get; private set; } =
        BreakBetweenPointsToolState.WaitingForTargetEntity;

    public EntityId? TargetEntityId => _targetEntityId;

    public Point2D? FirstBreakPoint => _firstBreakPoint;

    public Point2D? CurrentSecondBreakPoint => _currentSecondBreakPoint;

    public bool HasPreview =>
        State == BreakBetweenPointsToolState.WaitingForSecondBreakPoint &&
        _previewSegments.Count > 0;

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
            _targetLine is null ||
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
            ? ToolResult.Updated("Break segment preview updated.")
            : ToolResult.None("Second break point must be different and inside the target line.");
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
            return ToolResult.None("Break Segment currently supports line entities only.");
        }

        if (!context.Document.IsEntitySelectable(line))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        _targetEntityId = line.Id;
        _targetLine = line;
        State = BreakBetweenPointsToolState.WaitingForFirstBreakPoint;

        Point2D basePoint = line.GetClosestPoint(pointer.ModelPoint);
        context.CurrentBasePoint = basePoint;

        return ToolResult.Started("Specify first break point on line.");
    }

    private ToolResult AcceptFirstBreakPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_targetLine is null)
        {
            throw new InvalidOperationException(
                "Cannot accept first break point before selecting a target line.");
        }

        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        double parameter = LineParameterService.GetParameter(
            _targetLine.Geometry,
            point,
            context.GeometryTolerance);

        if (!LineIntersectionService.IsParameterOnSegment(parameter, context.GeometryTolerance))
        {
            return ToolResult.None("First break point must be on the target line.");
        }

        _firstBreakPoint = LineParameterService.PointAt(
            _targetLine.Geometry,
            parameter);

        _currentSecondBreakPoint = null;
        _previewSegments = Array.Empty<LineEntity>();
        State = BreakBetweenPointsToolState.WaitingForSecondBreakPoint;
        context.CurrentBasePoint = _firstBreakPoint;

        return ToolResult.Updated("Specify second break point on line.");
    }

    private ToolResult AcceptSecondBreakPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_targetLine is null ||
            _firstBreakPoint is null)
        {
            throw new InvalidOperationException(
                "Cannot accept second break point before selecting a target line and first break point.");
        }

        Point2D point = ResolvePoint(
            context,
            pointer.ModelPoint);

        IReadOnlyList<LineEntity> segments = LineBreakService.BreakBetweenPoints(
            _targetLine,
            _firstBreakPoint.Value,
            point,
            context.GeometryTolerance);

        if (segments.Count == 0)
        {
            return ToolResult.None(
                "Break points must be different and inside the target line.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { _targetLine },
                segments,
                "Break line between points"));

        Reset(context);

        return ToolResult.Completed("Line segment removed.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_targetLine is null ||
            _firstBreakPoint is null)
        {
            _currentSecondBreakPoint = null;
            _previewSegments = Array.Empty<LineEntity>();
            return;
        }

        double parameter = LineParameterService.GetParameter(
            _targetLine.Geometry,
            point,
            context.GeometryTolerance);

        _currentSecondBreakPoint = LineParameterService.PointAt(
            _targetLine.Geometry,
            parameter);

        _previewSegments = LineBreakService.BreakBetweenPoints(
            _targetLine,
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
        _targetLine = null;
        _firstBreakPoint = null;
        _currentSecondBreakPoint = null;
        _previewSegments = Array.Empty<LineEntity>();
        State = BreakBetweenPointsToolState.WaitingForTargetEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
