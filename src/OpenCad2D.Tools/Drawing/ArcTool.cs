using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw circular arc entities by center, start point and end point.
/// </summary>
public sealed class ArcTool : ICadTool, IToolPreviewEntityProvider
{
    private Point2D? _centerPoint;
    private Point2D? _startPoint;
    private Point2D? _currentPoint;

    public string Name => "Arc";

    public ArcToolState State { get; private set; } =
        ArcToolState.WaitingForCenterPoint;

    public Point2D? CenterPoint => _centerPoint;

    public Point2D? StartPoint => _startPoint;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        State == ArcToolState.WaitingForEndPoint &&
        _centerPoint.HasValue &&
        _startPoint.HasValue &&
        _currentPoint.HasValue;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            ArcToolState.WaitingForCenterPoint => SelectCenterPoint(
                context,
                pointer),

            ArcToolState.WaitingForStartPoint => SelectStartPoint(
                context,
                pointer),

            ArcToolState.WaitingForEndPoint => SelectEndPoint(
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

        if (State == ArcToolState.WaitingForCenterPoint)
        {
            return ToolResult.None();
        }

        if (_centerPoint is null)
        {
            return ToolResult.None();
        }

        _currentPoint = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _centerPoint);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Arc command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Arc tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ArcEntity? preview = GetPreviewEntity();
        return preview is null
            ? Array.Empty<CadEntity>()
            : new CadEntity[] { preview };
    }

    public ArcEntity? GetPreviewEntity()
    {
        if (!HasPreview ||
            _centerPoint is null ||
            _startPoint is null ||
            _currentPoint is null)
        {
            return null;
        }

        double radius = _centerPoint.Value.DistanceTo(_startPoint.Value);

        if (Tolerance.IsZero(radius))
        {
            return null;
        }

        Vector2D startVector = _centerPoint.Value.VectorTo(_startPoint.Value);
        Vector2D endVector = _centerPoint.Value.VectorTo(_currentPoint.Value);

        if (Tolerance.IsZero(startVector.Length) ||
            Tolerance.IsZero(endVector.Length))
        {
            return null;
        }

        Angle startAngle = Angle.FromRadians(
            Math.Atan2(
                startVector.Y,
                startVector.X));

        Angle endAngle = Angle.FromRadians(
            Math.Atan2(
                endVector.Y,
                endVector.X));

        return new ArcEntity(
            _centerPoint.Value,
            radius,
            startAngle,
            endAngle,
            isCounterClockwise: true);
    }

    private ToolResult SelectCenterPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            basePoint: null);

        _centerPoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = point;
        State = ArcToolState.WaitingForStartPoint;

        return ToolResult.Started("Specify arc start point.");
    }

    private ToolResult SelectStartPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_centerPoint is null)
        {
            throw new InvalidOperationException(
                "Arc tool is waiting for a start point but center point is missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _centerPoint);

        if (context.GeometryTolerance.ArePointsEqual(
                _centerPoint.Value,
                point))
        {
            return ToolResult.None(
                "Arc radius must be greater than zero.");
        }

        _startPoint = point;
        _currentPoint = point;
        State = ArcToolState.WaitingForEndPoint;

        return ToolResult.Started("Specify arc end point.");
    }

    private ToolResult SelectEndPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_centerPoint is null || _startPoint is null)
        {
            throw new InvalidOperationException(
                "Arc tool is waiting for an end point but center or start point is missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _centerPoint);

        if (context.GeometryTolerance.ArePointsEqual(
                _centerPoint.Value,
                point))
        {
            return ToolResult.None(
                "Arc end point must be different from center point.");
        }

        Vector2D startVector = _centerPoint.Value.VectorTo(_startPoint.Value);
        Vector2D endVector = _centerPoint.Value.VectorTo(point);

        if (context.GeometryTolerance.AreDistancesEqual(
                startVector.Length,
                0) ||
            context.GeometryTolerance.AreDistancesEqual(
                endVector.Length,
                0))
        {
            return ToolResult.None(
                "Arc radius must be greater than zero.");
        }

        Angle startAngle = Angle.FromRadians(
            Math.Atan2(
                startVector.Y,
                startVector.X));

        Angle endAngle = Angle.FromRadians(
            Math.Atan2(
                endVector.Y,
                endVector.X));

        if (context.GeometryTolerance.AreDistancesEqual(
                GetPositiveSweepRadians(startAngle.Radians, endAngle.Radians),
                0))
        {
            return ToolResult.None(
                "Arc end angle must be different from start angle.");
        }

        var arc = new ArcEntity(
            _centerPoint.Value,
            startVector.Length,
            startAngle,
            endAngle,
            isCounterClockwise: true,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(arc));

        Reset(context);

        return ToolResult.Completed("Arc created.");
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

    private void Reset(ToolContext? context = null)
    {
        _centerPoint = null;
        _startPoint = null;
        _currentPoint = null;
        State = ArcToolState.WaitingForCenterPoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }

    private static double GetPositiveSweepRadians(
        double startRadians,
        double endRadians)
    {
        double delta = endRadians - startRadians;
        delta %= 2.0 * Math.PI;

        if (delta < 0)
        {
            delta += 2.0 * Math.PI;
        }

        return delta;
    }
}
