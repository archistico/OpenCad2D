using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Navigation;

/// <summary>
/// Interactive two-point navigation tool used by the UI viewport to fit a user-drawn window.
/// </summary>
public sealed class ZoomWindowTool : ICadTool, ICommandDrivenTool, IToolPreviewDescriptorProvider
{
    private Point2D? _firstPoint;
    private Point2D? _currentPoint;
    private BoundingBox2D? _completedWindow;

    public string Name => "ZoomWindow";

    public Point2D? FirstPoint => _firstPoint;

    public Point2D? CurrentPoint => _currentPoint;

    public BoundingBox2D? CompletedWindow => _completedWindow;

    public bool HasPreview =>
        _firstPoint.HasValue &&
        _currentPoint.HasValue;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _firstPoint is null
            ? new CommandPromptState(
                "ZOOM WINDOW",
                "Specify first corner",
                CommandInputKind.Point,
                placeholder: "100,50")
            : new CommandPromptState(
                "ZOOM WINDOW",
                "Specify opposite corner",
                CommandInputKind.Point,
                placeholder: "100,50");
    }

    public BoundingBox2D? GetPreviewWindow()
    {
        if (!HasPreview || _firstPoint is null || _currentPoint is null)
        {
            return null;
        }

        return BoundingBox2D.FromPoints(
            _firstPoint.Value,
            _currentPoint.Value);
    }


    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        BoundingBox2D? window = GetPreviewWindow();

        if (window is null)
        {
            return ToolPreviewDescriptor.Empty;
        }

        return new ToolPreviewDescriptor(
            windows: new[]
            {
                new ToolPreviewWindow(
                    window.Value,
                    ToolPreviewWindowKind.Zoom)
            });
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

        _completedWindow = null;

        if (_firstPoint is null)
        {
            _firstPoint = pointer.ModelPoint;
            _currentPoint = pointer.ModelPoint;

            return ToolResult.Started("Zoom window: specify opposite corner.");
        }

        return Complete(pointer.ModelPoint);
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (_firstPoint is null)
        {
            return ToolResult.None();
        }

        _currentPoint = pointer.ModelPoint;

        return ToolResult.Updated("Zoom window updated.");
    }

    public ToolResult OnPointerReleased(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (_firstPoint is null)
        {
            return ToolResult.None();
        }

        if (!HasMovedFromFirstPoint(pointer.ModelPoint))
        {
            return ToolResult.Started("Zoom window: specify opposite corner.");
        }

        return Complete(pointer.ModelPoint);
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset();

        return ToolResult.Cancelled("Zoom window cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset();

        return ToolResult.None("Zoom window deactivated.");
    }

    public void ClearCompletedWindow()
    {
        _completedWindow = null;
    }

    private bool HasMovedFromFirstPoint(Point2D point)
    {
        return _firstPoint is not null &&
            (_firstPoint.Value.X != point.X ||
             _firstPoint.Value.Y != point.Y);
    }

    private ToolResult Complete(Point2D secondPoint)
    {
        if (_firstPoint is null)
        {
            return ToolResult.None();
        }

        _currentPoint = secondPoint;
        _completedWindow = BoundingBox2D.FromPoints(
            _firstPoint.Value,
            secondPoint);

        _firstPoint = null;
        _currentPoint = null;

        return ToolResult.Completed("Zoom window applied.");
    }

    private void Reset()
    {
        _firstPoint = null;
        _currentPoint = null;
        _completedWindow = null;
    }
}
