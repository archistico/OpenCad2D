using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Dimensions;

/// <summary>
/// Creates non-associative angular dimensions. The fourth click chooses the arc side,
/// allowing both minor and reflex angles.
/// </summary>
public sealed class AngularDimensionTool : ICadTool
{
    private Point2D? _center;
    private Point2D? _firstRayPoint;
    private Point2D? _secondRayPoint;
    private Point2D? _currentPoint;

    public string Name => "Angular Dimension";

    public Point2D? Center => _center;

    public Point2D? FirstRayPoint => _firstRayPoint;

    public Point2D? SecondRayPoint => _secondRayPoint;

    public Point2D? CurrentPoint => _currentPoint;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            _secondRayPoint ?? _firstRayPoint ?? _center);

        if (_center is null)
        {
            _center = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify first angle ray point.");
        }

        if (_firstRayPoint is null)
        {
            if (context.GeometryTolerance.ArePointsEqual(_center.Value, point))
            {
                return ToolResult.None("First angle ray point must be different from center.");
            }

            _firstRayPoint = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify second angle ray point.");
        }

        if (_secondRayPoint is null)
        {
            if (context.GeometryTolerance.ArePointsEqual(_center.Value, point))
            {
                return ToolResult.None("Second angle ray point must be different from center.");
            }

            double counterClockwiseSweep = AngularDimensionEntity.GetSweepDegrees(
                _center.Value,
                _firstRayPoint.Value,
                point,
                true);

            if (Tolerance.IsZero(counterClockwiseSweep) ||
                Math.Abs(counterClockwiseSweep - 360.0) <= context.GeometryTolerance.Distance)
            {
                return ToolResult.None("Second angle ray must use a different direction.");
            }

            _secondRayPoint = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify dimension arc position.");
        }

        if (context.GeometryTolerance.ArePointsEqual(_center.Value, point))
        {
            return ToolResult.None("Dimension arc point must be different from center.");
        }

        bool isCounterClockwise = AngularDimensionEntity.ShouldUseCounterClockwiseSweep(
            _center.Value,
            _firstRayPoint.Value,
            _secondRayPoint.Value,
            point);

        var dimension = new AngularDimensionEntity(
            _center.Value,
            _firstRayPoint.Value,
            _secondRayPoint.Value,
            point,
            isCounterClockwise,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(dimension));

        Reset(context);

        return ToolResult.Completed("Angular dimension created.");
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (_center is null)
        {
            return ToolResult.None();
        }

        _currentPoint = ApplySnap(
            context,
            pointer.ModelPoint,
            _secondRayPoint ?? _firstRayPoint ?? _center);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Angular Dimension command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Angular Dimension tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        if (_center is null || _currentPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        if (_firstRayPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        if (_secondRayPoint is null)
        {
            return new CadEntity[]
            {
                new LineEntity(_center.Value, _firstRayPoint.Value),
                new LineEntity(_center.Value, _currentPoint.Value)
            };
        }

        if (_center.Value.DistanceTo(_currentPoint.Value) <= double.Epsilon)
        {
            return new CadEntity[]
            {
                new LineEntity(_center.Value, _firstRayPoint.Value),
                new LineEntity(_center.Value, _secondRayPoint.Value)
            };
        }

        bool isCounterClockwise = AngularDimensionEntity.ShouldUseCounterClockwiseSweep(
            _center.Value,
            _firstRayPoint.Value,
            _secondRayPoint.Value,
            _currentPoint.Value);

        return new CadEntity[]
        {
            new AngularDimensionEntity(
                _center.Value,
                _firstRayPoint.Value,
                _secondRayPoint.Value,
                _currentPoint.Value,
                isCounterClockwise)
        };
    }

    private void Reset(ToolContext? context = null)
    {
        _center = null;
        _firstRayPoint = null;
        _secondRayPoint = null;
        _currentPoint = null;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
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
}
