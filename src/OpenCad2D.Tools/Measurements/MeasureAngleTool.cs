using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Measurements;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Measurements;

/// <summary>
/// Non-destructive tool that measures the angle defined by three points:
/// first ray point, vertex, second ray point.
/// </summary>
public sealed class MeasureAngleTool : ICadTool, IToolPreviewDescriptorProvider
{
    private Point2D? _firstRayPoint;
    private Point2D? _vertex;
    private Point2D? _currentPoint;

    public string Name => "Measure Angle";

    public MeasureAngleToolState State { get; private set; } =
        MeasureAngleToolState.WaitingForFirstRayPoint;

    public Point2D? FirstRayPoint => _firstRayPoint;

    public Point2D? Vertex => _vertex;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        _currentPoint.HasValue &&
        State != MeasureAngleToolState.WaitingForFirstRayPoint;

    public IReadOnlyList<LineEntity> GetPreviewEntities()
    {
        var entities = new List<LineEntity>();

        if (_firstRayPoint is null || _currentPoint is null)
        {
            return entities;
        }

        if (_vertex is null)
        {
            entities.Add(new LineEntity(
                _firstRayPoint.Value,
                _currentPoint.Value));

            return entities;
        }

        entities.Add(new LineEntity(
            _vertex.Value,
            _firstRayPoint.Value));

        entities.Add(new LineEntity(
            _vertex.Value,
            _currentPoint.Value));

        return entities;
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<CadEntity> entities = GetPreviewEntities()
            .Cast<CadEntity>()
            .ToList();

        var markers = new List<ToolPreviewMarker>();

        if (_firstRayPoint is not null)
        {
            markers.Add(new ToolPreviewMarker(
                _firstRayPoint.Value,
                ToolPreviewMarkerKind.Primary));
        }

        if (_vertex is not null)
        {
            markers.Add(new ToolPreviewMarker(
                _vertex.Value,
                ToolPreviewMarkerKind.Primary));
        }

        return new ToolPreviewDescriptor(
            entities: entities,
            markers: markers);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            GetConstraintBasePoint());

        point = ApplyAngleConstraintIfNeeded(
            context,
            point);

        return State switch
        {
            MeasureAngleToolState.WaitingForFirstRayPoint =>
                AcceptFirstRayPoint(context, point),

            MeasureAngleToolState.WaitingForVertex =>
                AcceptVertex(context, point),

            MeasureAngleToolState.WaitingForSecondRayPoint =>
                AcceptSecondRayPoint(context, point),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State == MeasureAngleToolState.WaitingForFirstRayPoint)
        {
            return ToolResult.None();
        }

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            GetConstraintBasePoint());

        _currentPoint = ApplyAngleConstraintIfNeeded(
            context,
            point);

        if (_firstRayPoint is not null && _vertex is not null)
        {
            if (AreSamePoint(_vertex.Value, _currentPoint.Value, context))
            {
                return ToolResult.None("Second ray point must be different from vertex.");
            }

            AngleMeasurement measurement = MeasurementService.MeasureAngle(
                _firstRayPoint.Value,
                _vertex.Value,
                _currentPoint.Value);

            return ToolResult.Updated(MeasurementFormatter.FormatAngle(measurement));
        }

        return ToolResult.Updated("Measure angle: specify vertex point.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Measure angle cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Measure angle tool deactivated.");
    }

    private ToolResult AcceptFirstRayPoint(
        ToolContext context,
        Point2D point)
    {
        _firstRayPoint = point;
        _currentPoint = point;
        State = MeasureAngleToolState.WaitingForVertex;
        context.CurrentBasePoint = point;

        return ToolResult.Started("Measure angle: specify vertex point.");
    }

    private ToolResult AcceptVertex(
        ToolContext context,
        Point2D point)
    {
        if (_firstRayPoint is null)
        {
            throw new InvalidOperationException(
                "Measure angle tool is waiting for vertex but first ray point is missing.");
        }

        if (AreSamePoint(_firstRayPoint.Value, point, context))
        {
            return ToolResult.None("Vertex must be different from first ray point.");
        }

        _vertex = point;
        _currentPoint = point;
        State = MeasureAngleToolState.WaitingForSecondRayPoint;
        context.CurrentBasePoint = point;

        return ToolResult.Started("Measure angle: specify second ray point.");
    }

    private ToolResult AcceptSecondRayPoint(
        ToolContext context,
        Point2D point)
    {
        if (_firstRayPoint is null || _vertex is null)
        {
            throw new InvalidOperationException(
                "Measure angle tool is waiting for second ray point but previous points are missing.");
        }

        if (AreSamePoint(_vertex.Value, point, context))
        {
            return ToolResult.None("Second ray point must be different from vertex.");
        }

        AngleMeasurement measurement = MeasurementService.MeasureAngle(
            _firstRayPoint.Value,
            _vertex.Value,
            point);

        string message = MeasurementFormatter.FormatAngle(measurement);

        Reset(context);

        return ToolResult.Completed(message);
    }

    private Point2D? GetConstraintBasePoint()
    {
        return State switch
        {
            MeasureAngleToolState.WaitingForVertex => _firstRayPoint,
            MeasureAngleToolState.WaitingForSecondRayPoint => _vertex,
            _ => null
        };
    }

    private Point2D ApplyAngleConstraintIfNeeded(
        ToolContext context,
        Point2D point)
    {
        Point2D? basePoint = GetConstraintBasePoint();

        if (basePoint is null)
        {
            return point;
        }

        return ToolInputConstraintService.ApplyAngleConstraint(
            context,
            basePoint.Value,
            point);
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

    private static bool AreSamePoint(
        Point2D first,
        Point2D second,
        ToolContext context)
    {
        return context.GeometryTolerance.ArePointsEqual(first, second);
    }

    private void Reset(ToolContext context)
    {
        _firstRayPoint = null;
        _vertex = null;
        _currentPoint = null;
        State = MeasureAngleToolState.WaitingForFirstRayPoint;
        context.CurrentBasePoint = null;
    }
}
