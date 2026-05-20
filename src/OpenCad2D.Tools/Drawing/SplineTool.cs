using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to create open or closed Bezier spline entities.
/// </summary>
public sealed class SplineTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider
{
    private readonly List<Point2D> _controlPoints = new();
    private Point2D? _currentPoint;

    public string Name => "Spline";

    public SplineToolState State { get; private set; } = SplineToolState.WaitingForFirstPoint;

    public IReadOnlyList<Point2D> ControlPoints => _controlPoints;

    public Point2D? CurrentPoint => _currentPoint;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State == SplineToolState.WaitingForFirstPoint)
        {
            return new CommandPromptState(
                "SPLINE",
                "Specify first control point",
                CommandInputKind.Point,
                placeholder: "100,50");
        }

        return new CommandPromptState(
            "SPLINE",
            "Specify next control point",
            CommandInputKind.PointOrDistanceOrOption,
            new[]
            {
                new CommandOption("Close", "C", "Close the spline"),
                new CommandOption("Undo", "U", "Remove the last spline control point")
            },
            acceptsEmptyEnter: true,
            placeholder: "100,50   |   @50,0   |   @100<45   |   distance   |   C   |   U");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return CompleteOpen(context);
        }

        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            return HandleOption(input.OptionKeyword, context);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "SPLINE expects a point input.");
        }

        return SubmitResolvedPoint(context, input.Point.Value);
    }

    public bool HasPreview =>
        State == SplineToolState.CollectingControlPoints &&
        _controlPoints.Count > 0 &&
        _currentPoint.HasValue;

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        BezierSplineEntity? preview = GetPreviewEntity();
        return preview is null
            ? Array.Empty<CadEntity>()
            : new CadEntity[] { preview };
    }

    public BezierSplineEntity? GetPreviewEntity()
    {
        if (!HasPreview)
        {
            return null;
        }

        List<Point2D> previewPoints = _controlPoints.ToList();
        if (_currentPoint is not null &&
            !AreSamePoint(previewPoints[^1], _currentPoint.Value))
        {
            previewPoints.Add(_currentPoint.Value);
        }

        return previewPoints.Count < 2
            ? null
            : new BezierSplineEntity(previewPoints);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ResolvePoint(context, pointer.ModelPoint);
        return SubmitResolvedPoint(context, point);
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != SplineToolState.CollectingControlPoints || _controlPoints.Count == 0)
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

        if (_controlPoints.Count < 2)
        {
            return ToolResult.None("Spline requires at least two control points.");
        }

        return Commit(context, false, "Spline created.");
    }

    public ToolResult CompleteClosed(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_controlPoints.Count < 3)
        {
            return ToolResult.None("Closed spline requires at least three control points.");
        }

        return Commit(context, true, "Closed spline created.");
    }

    public ToolResult UndoLastControlPoint(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State != SplineToolState.CollectingControlPoints || _controlPoints.Count == 0)
        {
            return ToolResult.None("Nothing to undo.");
        }

        _controlPoints.RemoveAt(_controlPoints.Count - 1);

        if (_controlPoints.Count == 0)
        {
            Reset(context);
            return ToolResult.Updated("Specify first spline control point.");
        }

        _currentPoint = _controlPoints[^1];
        context.CurrentBasePoint = _controlPoints[^1];
        return ToolResult.Updated("Specify next spline control point, press Enter/right-click to finish, or C to close.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.Cancelled("Spline command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.None("Spline tool deactivated.");
    }

    private ToolResult HandleOption(string? optionKeyword, ToolContext context)
    {
        return optionKeyword?.ToUpperInvariant() switch
        {
            "CLOSE" => CompleteClosed(context),
            "UNDO" => UndoLastControlPoint(context),
            _ => ToolResult.None("Unknown spline option.")
        };
    }

    private ToolResult SubmitResolvedPoint(ToolContext context, Point2D point)
    {
        if (State == SplineToolState.WaitingForFirstPoint)
        {
            _controlPoints.Add(point);
            _currentPoint = point;
            State = SplineToolState.CollectingControlPoints;
            context.CurrentBasePoint = point;
            return ToolResult.Started("Specify next spline control point, press Enter/right-click to finish, or C to close.");
        }

        if (_controlPoints.Count > 0 &&
            AreSamePoint(_controlPoints[^1], point, context.GeometryTolerance))
        {
            return ToolResult.None("Spline control point must be different from previous point.");
        }

        _controlPoints.Add(point);
        _currentPoint = point;
        context.CurrentBasePoint = point;
        return ToolResult.Updated("Specify next spline control point, press Enter/right-click to finish, or C to close.");
    }

    private ToolResult Commit(ToolContext context, bool isClosed, string message)
    {
        var spline = new BezierSplineEntity(
            _controlPoints.ToList(),
            isClosed,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(spline));

        Reset(context);
        return ToolResult.Completed(message);
    }

    private Point2D ResolvePoint(ToolContext context, Point2D cursorPoint)
    {
        Point2D? basePoint = _controlPoints.Count > 0 ? _controlPoints[^1] : null;
        Point2D point = ApplySnap(context, cursorPoint, basePoint);

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
        _controlPoints.Clear();
        _currentPoint = null;
        State = SplineToolState.WaitingForFirstPoint;
        context.CurrentBasePoint = null;
    }

    private static bool AreSamePoint(Point2D first, Point2D second)
    {
        return GeometryTolerance.Default.ArePointsEqual(first, second);
    }

    private static bool AreSamePoint(Point2D first, Point2D second, GeometryTolerance tolerance)
    {
        return tolerance.ArePointsEqual(first, second);
    }
}
