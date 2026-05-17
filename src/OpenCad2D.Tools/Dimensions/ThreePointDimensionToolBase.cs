using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Dimensions;

/// <summary>
/// Base class for non-associative dimension tools that collect two measured points
/// and one placement point.
/// </summary>
public abstract class ThreePointDimensionToolBase : ICadTool, IToolPreviewEntityProvider
{
    private Point2D? _firstPoint;
    private Point2D? _secondPoint;
    private Point2D? _currentPoint;

    public abstract string Name { get; }

    public Point2D? FirstPoint => _firstPoint;

    public Point2D? SecondPoint => _secondPoint;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasMeasurementPreview =>
        _firstPoint.HasValue &&
        !_secondPoint.HasValue &&
        _currentPoint.HasValue;

    public bool HasDimensionPreview =>
        _firstPoint.HasValue &&
        _secondPoint.HasValue &&
        _currentPoint.HasValue;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ApplySnap(context, pointer.ModelPoint, _secondPoint ?? _firstPoint);

        if (_firstPoint is null)
        {
            _firstPoint = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify second dimension point.");
        }

        if (_secondPoint is null)
        {
            if (!IsValidSecondPoint(
                    context,
                    _firstPoint.Value,
                    point,
                    out string? validationMessage))
            {
                return ToolResult.None(validationMessage);
            }

            _secondPoint = point;
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify dimension line position.");
        }

        ToolResult result = CreateDimension(
            context,
            _firstPoint.Value,
            _secondPoint.Value,
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

        if (_firstPoint is null)
        {
            return ToolResult.None();
        }

        _currentPoint = ApplySnap(context, pointer.ModelPoint, _secondPoint ?? _firstPoint);

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

    IReadOnlyList<CadEntity> IToolPreviewEntityProvider.GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetPreviewEntities();
    }

    protected virtual bool IsValidSecondPoint(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint,
        out string? validationMessage)
    {
        if (context.GeometryTolerance.ArePointsEqual(firstPoint, secondPoint))
        {
            validationMessage = "Second dimension point must be different from first point.";
            return false;
        }

        validationMessage = null;
        return true;
    }

    protected abstract ToolResult CreateDimension(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint,
        Point2D dimensionLinePoint);

    protected void Reset(ToolContext? context = null)
    {
        _firstPoint = null;
        _secondPoint = null;
        _currentPoint = null;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }

    protected Point2D ApplySnap(
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
