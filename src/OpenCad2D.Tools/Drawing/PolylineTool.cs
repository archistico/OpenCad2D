using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw open or closed polylines.
/// </summary>
public sealed class PolylineTool : ICadTool
{
    private readonly List<Point2D> _vertices = new();
    private Point2D? _currentPoint;

    public string Name => "Polyline";

    public PolylineToolState State { get; private set; } =
        PolylineToolState.WaitingForFirstPoint;

    public IReadOnlyList<Point2D> Vertices => _vertices;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        State == PolylineToolState.CollectingVertices &&
        _vertices.Count > 0 &&
        _currentPoint.HasValue;

    public PolylineEntity? GetPreviewEntity()
    {
        if (!HasPreview)
        {
            return null;
        }

        List<Point2D> previewVertices = _vertices.ToList();

        if (_currentPoint is not null &&
            !AreSamePoint(previewVertices[^1], _currentPoint.Value))
        {
            previewVertices.Add(_currentPoint.Value);
        }

        if (previewVertices.Count < 2)
        {
            return null;
        }

        return new PolylineEntity(previewVertices);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ResolvePoint(context, pointer.ModelPoint);

        if (State == PolylineToolState.WaitingForFirstPoint)
        {
            _vertices.Add(point);
            _currentPoint = point;
            State = PolylineToolState.CollectingVertices;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify next polyline point, press Enter to finish, or C to close.");
        }

        if (State == PolylineToolState.CollectingVertices)
        {
            if (_vertices.Count > 0 &&
                AreSamePoint(_vertices[^1], point, context.GeometryTolerance))
            {
                return ToolResult.None("Polyline point must be different from previous point.");
            }

            _vertices.Add(point);
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Updated("Specify next polyline point, press Enter to finish, or C to close.");
        }

        return ToolResult.None();
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != PolylineToolState.CollectingVertices ||
            _vertices.Count == 0)
        {
            return ToolResult.None();
        }

        _currentPoint = ResolvePoint(context, pointer.ModelPoint);

        return ToolResult.Updated();
    }

    public ToolResult OnPointerReleased(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return ToolResult.None();
    }

    public ToolResult CompleteOpen(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_vertices.Count < 2)
        {
            return ToolResult.None("Polyline requires at least two points.");
        }

        return Commit(
            context,
            isClosed: false,
            message: "Polyline created.");
    }

    public ToolResult CompleteClosed(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_vertices.Count < 3)
        {
            return ToolResult.None("Closed polyline requires at least three points.");
        }

        return Commit(
            context,
            isClosed: true,
            message: "Closed polyline created.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Polyline command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Polyline tool deactivated.");
    }

    private ToolResult Commit(
        ToolContext context,
        bool isClosed,
        string message)
    {
        var polyline = new PolylineEntity(
            _vertices.ToList(),
            isClosed,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(polyline));

        Reset(context);

        return ToolResult.Completed(message);
    }

    private Point2D ResolvePoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        Point2D? basePoint = _vertices.Count > 0
            ? _vertices[^1]
            : null;

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

    private void Reset(ToolContext context)
    {
        _vertices.Clear();
        _currentPoint = null;
        State = PolylineToolState.WaitingForFirstPoint;
        context.CurrentBasePoint = null;
    }

    private static bool AreSamePoint(Point2D first, Point2D second)
    {
        return GeometryTolerance.Default.ArePointsEqual(first, second);
    }

    private static bool AreSamePoint(
        Point2D first,
        Point2D second,
        GeometryTolerance tolerance)
    {
        return tolerance.ArePointsEqual(first, second);
    }
}
