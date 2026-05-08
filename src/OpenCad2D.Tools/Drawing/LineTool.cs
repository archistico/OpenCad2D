using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw line entities.
/// </summary>
public sealed class LineTool : ICadTool
{
    private Point2D? _startPoint;
    private Point2D? _currentPoint;

    public string Name => "Line";

    public LineToolState State { get; private set; } =
        LineToolState.WaitingForFirstPoint;

    public Point2D? StartPoint => _startPoint;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        _startPoint.HasValue &&
        _currentPoint.HasValue &&
        State == LineToolState.WaitingForSecondPoint;

    public LineEntity? GetPreviewEntity()
    {
        if (!HasPreview)
        {
            return null;
        }

        return new LineEntity(
            _startPoint!.Value,
            _currentPoint!.Value);
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
            _startPoint);

        if (State == LineToolState.WaitingForFirstPoint)
        {
            _startPoint = point;
            _currentPoint = point;
            State = LineToolState.WaitingForSecondPoint;

            return ToolResult.Started("Specify next point.");
        }

        if (State == LineToolState.WaitingForSecondPoint)
        {
            if (_startPoint is null)
            {
                throw new InvalidOperationException(
                    "Line tool is waiting for second point but start point is missing.");
            }

            if (IsSamePoint(_startPoint.Value, point))
            {
                return ToolResult.None("Line length cannot be zero.");
            }

            var line = new LineEntity(
                _startPoint.Value,
                point);

            context.CommandHistory.Execute(
                context.Document,
                new AddEntityCommand(line));

            _startPoint = null;
            _currentPoint = null;
            State = LineToolState.WaitingForFirstPoint;

            return ToolResult.Completed("Line created.");
        }

        return ToolResult.None();
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != LineToolState.WaitingForSecondPoint)
        {
            return ToolResult.None();
        }

        _currentPoint = ApplySnap(
            context,
            pointer.ModelPoint,
            _startPoint);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _startPoint = null;
        _currentPoint = null;
        State = LineToolState.WaitingForFirstPoint;

        return ToolResult.Cancelled("Line command cancelled.");
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
            basePoint);

        SnapCandidate? candidate = context.SnapService.Snap(request);

        return candidate?.Point ?? cursorPoint;
    }

    private static bool IsSamePoint(Point2D first, Point2D second)
    {
        return Tolerance.AreEqual(first.X, second.X)
            && Tolerance.AreEqual(first.Y, second.Y);
    }
}