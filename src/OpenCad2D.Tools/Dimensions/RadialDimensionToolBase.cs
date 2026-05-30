using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Dimensions;

/// <summary>
/// Base class for non-associative radius and diameter dimension tools.
/// </summary>
public abstract class RadialDimensionToolBase : ICadTool, ICommandDrivenTool
{
    private Point2D? _center;
    private Point2D? _pointOnCircle;
    private Point2D? _currentPoint;

    public abstract string Name { get; }

    public Point2D? Center => _center;

    public Point2D? PointOnCircle => _pointOnCircle;

    public Point2D? CurrentPoint => _currentPoint;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string commandName = Name.ToUpperInvariant();

        if (_center is null)
        {
            return new CommandPromptState(
                commandName,
                "Specify center point",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0");
        }

        if (_pointOnCircle is null)
        {
            return new CommandPromptState(
                commandName,
                "Specify point on circle",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   radius");
        }

        return new CommandPromptState(
            commandName,
            "Specify dimension text position",
            CommandInputKind.PointOrDistance,
            placeholder: "100,50   |   @50,0   |   distance");
    }


    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? $"{Name} expects a point input.");
        }

        return OnPointerPressed(
            context,
            new PointerInfo(input.Point.Value));
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ApplySnap(context, pointer.ModelPoint, _pointOnCircle ?? _center);

        if (_center is null)
        {
            _center = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify point on circle.");
        }

        if (_pointOnCircle is null)
        {
            if (context.GeometryTolerance.ArePointsEqual(_center.Value, point))
            {
                return ToolResult.None("Point on circle must be different from center.");
            }

            _pointOnCircle = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify dimension text position.");
        }

        ToolResult result = CreateDimension(
            context,
            _center.Value,
            _pointOnCircle.Value,
            point);

        Reset(context);

        return result;
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

        _currentPoint = ApplySnap(context, pointer.ModelPoint, _pointOnCircle ?? _center);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled($"{Name} command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None($"{Name} tool deactivated.");
    }

    public abstract IReadOnlyList<CadEntity> GetPreviewEntities();

    protected abstract ToolResult CreateDimension(
        ToolContext context,
        Point2D center,
        Point2D pointOnCircle,
        Point2D textPoint);

    protected void Reset(ToolContext? context = null)
    {
        _center = null;
        _pointOnCircle = null;
        _currentPoint = null;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }

    private Point2D ApplySnap(
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
