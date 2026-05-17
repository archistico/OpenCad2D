using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw a rectangular closed polyline from a start point,
/// a first side endpoint and a point defining the opposite side distance.
/// </summary>
public sealed class RectangleBySidesTool : ICadTool, IToolPreviewEntityProvider
{
    private Point2D? _startPoint;
    private Point2D? _firstSideEndPoint;
    private Point2D? _currentPoint;

    public string Name => "Rectangle Sides";

    public RectangleBySidesToolState State { get; private set; } =
        RectangleBySidesToolState.WaitingForStartPoint;

    public Point2D? StartPoint => _startPoint;

    public Point2D? FirstSideEndPoint => _firstSideEndPoint;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        GetFirstSidePreviewEntity() is not null ||
        GetPreviewEntity() is not null;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            RectangleBySidesToolState.WaitingForStartPoint => SelectStartPoint(
                context,
                pointer),

            RectangleBySidesToolState.WaitingForFirstSideEndPoint => SelectFirstSideEndPoint(
                context,
                pointer),

            RectangleBySidesToolState.WaitingForSecondSidePoint => SelectSecondSidePoint(
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

        if (State == RectangleBySidesToolState.WaitingForStartPoint)
        {
            return ToolResult.None();
        }

        Point2D? basePoint = State == RectangleBySidesToolState.WaitingForFirstSideEndPoint
            ? _startPoint
            : _startPoint;

        if (basePoint is null)
        {
            return ToolResult.None();
        }

        _currentPoint = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            basePoint,
            applyAngleConstraint: State == RectangleBySidesToolState.WaitingForFirstSideEndPoint);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Rectangle Sides command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Rectangle Sides tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previews = new List<CadEntity>();

        LineEntity? firstSidePreview = GetFirstSidePreviewEntity();
        if (firstSidePreview is not null)
        {
            previews.Add(firstSidePreview);
        }

        PolylineEntity? rectanglePreview = GetPreviewEntity();
        if (rectanglePreview is not null)
        {
            previews.Add(rectanglePreview);
        }

        return previews;
    }

    public LineEntity? GetFirstSidePreviewEntity()
    {
        if (State != RectangleBySidesToolState.WaitingForFirstSideEndPoint ||
            _startPoint is null ||
            _currentPoint is null)
        {
            return null;
        }

        if (OpenCad2D.Geometry.Tolerance.IsZero(
                _startPoint.Value.DistanceTo(_currentPoint.Value)))
        {
            return null;
        }

        return new LineEntity(
            _startPoint.Value,
            _currentPoint.Value);
    }

    public PolylineEntity? GetPreviewEntity()
    {
        if (_startPoint is null ||
            _firstSideEndPoint is null ||
            _currentPoint is null)
        {
            return null;
        }

        return TryCreateRectangleEntity(
            _startPoint.Value,
            _firstSideEndPoint.Value,
            _currentPoint.Value,
            layerId: null,
            out PolylineEntity? rectangle)
            ? rectangle
            : null;
    }

    private ToolResult SelectStartPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            basePoint: null,
            applyAngleConstraint: false);

        _startPoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = point;
        State = RectangleBySidesToolState.WaitingForFirstSideEndPoint;

        return ToolResult.Started("Specify first side endpoint.");
    }

    private ToolResult SelectFirstSideEndPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_startPoint is null)
        {
            throw new InvalidOperationException(
                "Rectangle Sides tool is waiting for first side endpoint but start point is missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _startPoint,
            applyAngleConstraint: true);

        if (context.GeometryTolerance.ArePointsEqual(
                _startPoint.Value,
                point))
        {
            return ToolResult.None("First side length must be greater than zero.");
        }

        _firstSideEndPoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = _startPoint;
        State = RectangleBySidesToolState.WaitingForSecondSidePoint;

        return ToolResult.Started("Specify second side point.");
    }

    private ToolResult SelectSecondSidePoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_startPoint is null || _firstSideEndPoint is null)
        {
            throw new InvalidOperationException(
                "Rectangle Sides tool is waiting for second side point but previous points are missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _startPoint,
            applyAngleConstraint: false);

        if (!TryCreateRectangleEntity(
                _startPoint.Value,
                _firstSideEndPoint.Value,
                point,
                context.Creation.CurrentLayerId,
                out PolylineEntity? rectangle))
        {
            return ToolResult.None("Second side length must be greater than zero.");
        }

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(rectangle));

        Reset(context);

        return ToolResult.Completed("Rectangle Sides created.");
    }

    private static bool TryCreateRectangleEntity(
        Point2D startPoint,
        Point2D firstSideEndPoint,
        Point2D secondSidePoint,
        OpenCad2D.Core.Identifiers.LayerId? layerId,
        out PolylineEntity rectangle)
    {
        Vector2D firstSide = startPoint.VectorTo(firstSideEndPoint);
        double firstSideLength = firstSide.Length;

        if (OpenCad2D.Geometry.Tolerance.IsZero(firstSideLength))
        {
            rectangle = null!;
            return false;
        }

        Vector2D firstSideDirection = firstSide / firstSideLength;
        Vector2D perpendicularDirection = firstSideDirection.PerpendicularLeft();
        Vector2D candidate = startPoint.VectorTo(secondSidePoint);
        double signedHeight = candidate.Dot(perpendicularDirection);

        if (OpenCad2D.Geometry.Tolerance.IsZero(signedHeight))
        {
            rectangle = null!;
            return false;
        }

        Vector2D secondSide = perpendicularDirection * signedHeight;

        var vertices = new[]
        {
            startPoint,
            firstSideEndPoint,
            firstSideEndPoint + secondSide,
            startPoint + secondSide
        };

        rectangle = new PolylineEntity(
            vertices,
            isClosed: true,
            layerId: layerId);

        return true;
    }

    private static Point2D ResolveInputPoint(
        ToolContext context,
        Point2D cursorPoint,
        Point2D? basePoint,
        bool applyAngleConstraint)
    {
        Point2D point = ApplySnap(
            context,
            cursorPoint,
            basePoint);

        if (applyAngleConstraint && basePoint is not null)
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
        _firstSideEndPoint = null;
        _currentPoint = null;
        State = RectangleBySidesToolState.WaitingForStartPoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
