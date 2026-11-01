using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw circular arc entities by center, start point and end point.
/// The arc runs counter-clockwise from the start point by default; the <c>Direction</c> option
/// (shortcut <c>D</c>, available in every phase and in the HUD) switches between counter-clockwise
/// and clockwise. The chosen direction is kept for the following arcs of the same command.
/// </summary>
public sealed class ArcTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider
{
    private static readonly CommandOption DirectionOption =
        new("Direction", "D", "Switch between counter-clockwise and clockwise");

    private Point2D? _centerPoint;
    private Point2D? _startPoint;
    private Point2D? _currentPoint;
    private bool _isCounterClockwise = true;

    public string Name => "Arc";

    public ArcToolState State { get; private set; } =
        ArcToolState.WaitingForCenterPoint;

    public bool IsCommandFinished { get; private set; }

    public Point2D? CenterPoint => _centerPoint;

    public Point2D? StartPoint => _startPoint;

    public Point2D? CurrentPoint => _currentPoint;

    /// <summary>
    /// Direction used for the preview and the created arc, from the start point to the end point.
    /// </summary>
    public bool IsCounterClockwise => _isCounterClockwise;

    public bool HasPreview =>
        State == ArcToolState.WaitingForEndPoint &&
        _centerPoint.HasValue &&
        _startPoint.HasValue &&
        _currentPoint.HasValue;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string direction = _isCounterClockwise
            ? "(counter-clockwise)"
            : "(clockwise)";
        CommandOption[] options = { DirectionOption };

        return State switch
        {
            ArcToolState.WaitingForCenterPoint => new CommandPromptState(
                "ARC",
                $"Specify center point {direction} or press Enter to finish",
                CommandInputKind.PointOrOption,
                options,
                acceptsEmptyEnter: true,
                placeholder: "100,50   |   @50,0   |   D direction   |   Enter finishes"),

            ArcToolState.WaitingForStartPoint => new CommandPromptState(
                "ARC",
                $"Specify start point or radius {direction}",
                CommandInputKind.PointOrDistanceOrOption,
                options,
                placeholder: "100,50   |   @50,0   |   radius   |   D direction"),

            ArcToolState.WaitingForEndPoint => new CommandPromptState(
                "ARC",
                $"Specify end point or end angle {direction}",
                CommandInputKind.PointOrAngleOrOption,
                options,
                placeholder: "100,50   |   @50,0   |   end angle   |   D direction"),

            _ => new CommandPromptState(
                "ARC",
                "Specify point",
                CommandInputKind.Point,
                placeholder: "100,50")
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, DirectionOption.Keyword, StringComparison.OrdinalIgnoreCase))
        {
            return ToggleDirection();
        }

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            if (State == ArcToolState.WaitingForCenterPoint)
            {
                Reset(context);
                IsCommandFinished = true;
                return ToolResult.Completed("Arc command finished.");
            }

            return ToolResult.None(State == ArcToolState.WaitingForStartPoint
                ? "Specify arc start point or radius."
                : "Specify arc end point or end angle.");
        }

        if (State == ArcToolState.WaitingForEndPoint &&
            input.Kind == CommandInputSubmissionKind.Angle &&
            input.AngleDegrees is not null)
        {
            return AcceptEndAngle(context, input.AngleDegrees.Value);
        }

        if (State == ArcToolState.WaitingForStartPoint &&
            input.Kind == CommandInputSubmissionKind.Distance &&
            input.Distance is not null)
        {
            return AcceptRadius(context, input.Distance.Value);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? $"{Name} expects a point input.");
        }

        // Typed coordinates are explicit: no snap, ortho or polar adjustment.
        return AcceptPoint(context, input.Point.Value);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            State == ArcToolState.WaitingForCenterPoint ? null : _centerPoint);

        return AcceptPoint(context, point);
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

        return new ArcEntity(
            _centerPoint.Value,
            radius,
            GetAngle(startVector),
            GetAngle(endVector),
            _isCounterClockwise);
    }

    private ToolResult ToggleDirection()
    {
        _isCounterClockwise = !_isCounterClockwise;

        return ToolResult.Started(_isCounterClockwise
            ? "Arc direction: counter-clockwise."
            : "Arc direction: clockwise.");
    }

    private ToolResult AcceptPoint(
        ToolContext context,
        Point2D point)
    {
        return State switch
        {
            ArcToolState.WaitingForCenterPoint => AcceptCenterPoint(context, point),
            ArcToolState.WaitingForStartPoint => AcceptStartPoint(context, point),
            ArcToolState.WaitingForEndPoint => AcceptEndPoint(context, point),
            _ => ToolResult.None()
        };
    }

    private ToolResult AcceptCenterPoint(
        ToolContext context,
        Point2D point)
    {
        _centerPoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = point;
        State = ArcToolState.WaitingForStartPoint;

        return ToolResult.Started("Specify arc start point.");
    }

    private ToolResult AcceptRadius(
        ToolContext context,
        double radius)
    {
        if (_centerPoint is null)
        {
            return ToolResult.None("Specify the arc center point first.");
        }

        if (radius <= 0 || context.GeometryTolerance.IsDistanceZero(radius))
        {
            return ToolResult.None("Arc radius must be greater than zero.");
        }

        // Without a direction the radius starts along the X axis of the current UCS.
        Vector2D direction = _currentPoint is { } current &&
                             !context.GeometryTolerance.ArePointsEqual(_centerPoint.Value, current)
            ? _centerPoint.Value.VectorTo(current).Normalize()
            : context.CurrentUcs.UserVectorToWorld(new Vector2D(1, 0)).Normalize();

        return AcceptStartPoint(context, _centerPoint.Value + direction * radius);
    }

    private ToolResult AcceptStartPoint(
        ToolContext context,
        Point2D point)
    {
        if (_centerPoint is null)
        {
            throw new InvalidOperationException(
                "Arc tool is waiting for a start point but center point is missing.");
        }

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

    private ToolResult AcceptEndAngle(
        ToolContext context,
        double angleDegrees)
    {
        if (_centerPoint is null || _startPoint is null)
        {
            return ToolResult.None("Specify the arc center and start point first.");
        }

        // The typed value is the direction of the end point from the center, measured in the
        // current UCS like the HUD Angle field.
        double radians = angleDegrees * Math.PI / 180.0;
        Vector2D direction = context.CurrentUcs
            .UserVectorToWorld(new Vector2D(Math.Cos(radians), Math.Sin(radians)))
            .Normalize();
        double radius = _centerPoint.Value.DistanceTo(_startPoint.Value);

        return AcceptEndPoint(context, _centerPoint.Value + direction * radius);
    }

    private ToolResult AcceptEndPoint(
        ToolContext context,
        Point2D point)
    {
        if (_centerPoint is null || _startPoint is null)
        {
            throw new InvalidOperationException(
                "Arc tool is waiting for an end point but center or start point is missing.");
        }

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

        Angle startAngle = GetAngle(startVector);
        Angle endAngle = GetAngle(endVector);

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
            _isCounterClockwise,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(arc));

        Reset(context);

        return ToolResult.Completed(_isCounterClockwise
            ? "Arc created."
            : "Clockwise arc created.");
    }

    private static Angle GetAngle(Vector2D vector)
    {
        return Angle.FromRadians(
            Math.Atan2(
                vector.Y,
                vector.X));
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

    /// <summary>
    /// Clears the points of the arc in progress. The chosen direction is kept so that a
    /// sequence of arcs drawn with the same command keeps the same orientation.
    /// </summary>
    private void Reset(ToolContext? context = null)
    {
        _centerPoint = null;
        _startPoint = null;
        _currentPoint = null;
        IsCommandFinished = false;
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
