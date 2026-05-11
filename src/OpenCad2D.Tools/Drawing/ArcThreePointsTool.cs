using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw circular arc entities through three points.
/// </summary>
public sealed class ArcThreePointsTool : ICadTool
{
    private Point2D? _startPoint;
    private Point2D? _pointOnArc;
    private Point2D? _currentPoint;

    public string Name => "Arc 3P";

    public ArcThreePointsToolState State { get; private set; } =
        ArcThreePointsToolState.WaitingForStartPoint;

    public Point2D? StartPoint => _startPoint;

    public Point2D? PointOnArc => _pointOnArc;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        State == ArcThreePointsToolState.WaitingForEndPoint &&
        _startPoint.HasValue &&
        _pointOnArc.HasValue &&
        _currentPoint.HasValue &&
        GetPreviewEntity() is not null;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            ArcThreePointsToolState.WaitingForStartPoint => SelectStartPoint(
                context,
                pointer),

            ArcThreePointsToolState.WaitingForPointOnArc => SelectPointOnArc(
                context,
                pointer),

            ArcThreePointsToolState.WaitingForEndPoint => SelectEndPoint(
                context,
                pointer),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State == ArcThreePointsToolState.WaitingForStartPoint)
        {
            return ToolResult.None();
        }

        Point2D? basePoint = State == ArcThreePointsToolState.WaitingForPointOnArc
            ? _startPoint
            : _pointOnArc;

        if (basePoint is null)
        {
            return ToolResult.None();
        }

        _currentPoint = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            basePoint);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Arc 3P command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Arc 3P tool deactivated.");
    }

    public ArcEntity? GetPreviewEntity()
    {
        if (_startPoint is null ||
            _pointOnArc is null ||
            _currentPoint is null)
        {
            return null;
        }

        if (!ArcCreationService.TryCreateFromThreePoints(
                _startPoint.Value,
                _pointOnArc.Value,
                _currentPoint.Value,
                out Arc2D arc))
        {
            return null;
        }

        return new ArcEntity(
            arc.Center,
            arc.Radius,
            arc.StartAngle,
            arc.EndAngle,
            arc.IsCounterClockwise);
    }

    private ToolResult SelectStartPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            basePoint: null);

        _startPoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = point;
        State = ArcThreePointsToolState.WaitingForPointOnArc;

        return ToolResult.Started("Specify point on arc.");
    }

    private ToolResult SelectPointOnArc(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_startPoint is null)
        {
            throw new InvalidOperationException(
                "Arc 3P tool is waiting for a point on arc but start point is missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _startPoint);

        if (context.GeometryTolerance.ArePointsEqual(
                _startPoint.Value,
                point))
        {
            return ToolResult.None(
                "Point on arc must be different from start point.");
        }

        _pointOnArc = point;
        _currentPoint = point;
        context.CurrentBasePoint = point;
        State = ArcThreePointsToolState.WaitingForEndPoint;

        return ToolResult.Started("Specify arc end point.");
    }

    private ToolResult SelectEndPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_startPoint is null || _pointOnArc is null)
        {
            throw new InvalidOperationException(
                "Arc 3P tool is waiting for an end point but previous points are missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _pointOnArc);

        if (!ArcCreationService.TryCreateFromThreePoints(
                _startPoint.Value,
                _pointOnArc.Value,
                point,
                context.GeometryTolerance,
                out Arc2D arc))
        {
            return ToolResult.None(
                "Cannot create an arc from duplicate or collinear points.");
        }

        var entity = new ArcEntity(
            arc.Center,
            arc.Radius,
            arc.StartAngle,
            arc.EndAngle,
            arc.IsCounterClockwise,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(entity));

        Reset(context);

        return ToolResult.Completed("Arc 3P created.");
    }

    private Point2D ResolveInputPoint(
        ToolContext context,
        Point2D cursorPoint,
        Point2D? basePoint)
    {
        Point2D point = ApplySnap(
            context,
            cursorPoint,
            basePoint);

        if (basePoint is not null)
        {
            point = ToolInputConstraintService.ApplyAngleConstraint(
                context,
                basePoint.Value,
                point);
        }

        return point;
    }

    private static Point2D ApplySnap(
        ToolContext context,
        Point2D cursorPoint,
        Point2D? basePoint)
    {
        if (context.EnabledSnaps == SnapKind.None ||
            context.GeometryTolerance.IsDistanceZero(context.SnapTolerance))
        {
            return cursorPoint;
        }

        var request = new SnapRequest(
            context.Document,
            cursorPoint,
            context.SnapTolerance,
            context.EnabledSnaps,
            basePoint,
            context.GridSettings);

        SnapCandidate? candidate = context.SnapService.Snap(request);

        return candidate?.Point ?? cursorPoint;
    }

    private void Reset(ToolContext? context = null)
    {
        _startPoint = null;
        _pointOnArc = null;
        _currentPoint = null;
        State = ArcThreePointsToolState.WaitingForStartPoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
