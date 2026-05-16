using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Base class for interactive tools that require two points.
/// Examples: line, move, copy, scale by reference, mirror by two points.
/// </summary>
public abstract class TwoPointToolBase : ICadTool
{
    private Point2D? _firstPoint;
    private Point2D? _currentPoint;

    public abstract string Name { get; }

    public TwoPointToolState State { get; private set; } =
        TwoPointToolState.WaitingForFirstPoint;

    public Point2D? FirstPoint => _firstPoint;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        _firstPoint.HasValue &&
        _currentPoint.HasValue &&
        State == TwoPointToolState.WaitingForSecondPoint;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            _firstPoint);

        if (State == TwoPointToolState.WaitingForSecondPoint &&
            _firstPoint is not null)
        {
            point = ToolInputConstraintService.ApplyAngleConstraint(
                context,
                _firstPoint.Value,
                point);
        }

        return SubmitResolvedPoint(context, point);
    }


    protected ToolResult SubmitResolvedPoint(
        ToolContext context,
        Point2D point)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State == TwoPointToolState.WaitingForFirstPoint)
        {
            _firstPoint = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;
            State = TwoPointToolState.WaitingForSecondPoint;

            return OnFirstPointSelected(context, point);
        }

        if (State == TwoPointToolState.WaitingForSecondPoint)
        {
            if (_firstPoint is null)
            {
                throw new InvalidOperationException(
                    "Tool is waiting for second point but first point is missing.");
            }

            if (AreSamePoint(
                    _firstPoint.Value,
                    point,
                    context))
            {
                return ToolResult.None(
                    "Second point must be different from first point.");
            }

            ToolResult result = OnSecondPointSelected(
                context,
                _firstPoint.Value,
                point);

            if (ShouldResetAfterSecondPoint(result))
            {
                Reset(context);
            }

            return result;
        }

        return ToolResult.None();
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != TwoPointToolState.WaitingForSecondPoint)
        {
            return ToolResult.None();
        }

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            _firstPoint);

        _currentPoint = ToolInputConstraintService.ApplyAngleConstraint(
            context,
            _firstPoint!.Value,
            point);

        return OnPreviewUpdated(
            context,
            _firstPoint.Value,
            _currentPoint.Value);
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled($"{Name} command cancelled.");
    }

    public virtual ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None($"{Name} tool deactivated.");
    }

    protected void Reset(ToolContext? context = null)
    {
        _firstPoint = null;
        _currentPoint = null;
        State = TwoPointToolState.WaitingForFirstPoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }

    protected virtual ToolResult OnFirstPointSelected(
        ToolContext context,
        Point2D firstPoint)
    {
        return ToolResult.Started(
            "Specify second point, type coordinates, or type distance.");
    }

    protected virtual ToolResult OnPreviewUpdated(
        ToolContext context,
        Point2D firstPoint,
        Point2D currentPoint)
    {
        return ToolResult.Updated();
    }

    protected abstract ToolResult OnSecondPointSelected(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint);

    protected virtual bool ShouldResetAfterSecondPoint(ToolResult result)
    {
        return result.Kind == ToolResultKind.Completed;
    }

    protected virtual Point2D ApplySnap(
        ToolContext context,
        Point2D cursorPoint,
        Point2D? basePoint)
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
            basePoint,
            context.GridSettings);

        SnapCandidate? candidate = context.SnapService.Snap(request);

        return candidate?.Point ?? cursorPoint;
    }

    private static bool AreSamePoint(
        Point2D first,
        Point2D second,
        ToolContext context)
    {
        return context.GeometryTolerance.ArePointsEqual(first, second);
    }
}
