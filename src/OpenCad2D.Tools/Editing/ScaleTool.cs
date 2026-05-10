using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Interactive tool used to uniformly scale the current selection around a base point.
/// </summary>
public sealed class ScaleTool : ICadTool
{
    private Point2D? _basePoint;
    private Point2D? _referencePoint;
    private Point2D? _currentDestinationPoint;
    private double _currentFactor = 1.0;

    public string Name => "Scale";

    public ScaleToolState State { get; private set; } =
        ScaleToolState.WaitingForBasePoint;

    public Point2D? BasePoint => _basePoint;

    public Point2D? ReferencePoint => _referencePoint;

    public Point2D? CurrentDestinationPoint => _currentDestinationPoint;

    public double CurrentFactor => _currentFactor;

    public bool HasPreview =>
        State == ScaleToolState.WaitingForDestinationPoint &&
        _basePoint.HasValue &&
        _referencePoint.HasValue &&
        _currentDestinationPoint.HasValue &&
        _currentFactor > 0;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (!context.Selection.HasSelection)
        {
            Reset(context);

            return ToolResult.None("No entities selected.");
        }

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            _basePoint);

        return State switch
        {
            ScaleToolState.WaitingForBasePoint =>
                AcceptBasePoint(context, point),

            ScaleToolState.WaitingForReferencePoint =>
                AcceptReferencePoint(context, point),

            ScaleToolState.WaitingForDestinationPoint =>
                AcceptDestinationPoint(context, point),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != ScaleToolState.WaitingForDestinationPoint ||
            _basePoint is null ||
            _referencePoint is null)
        {
            return ToolResult.None();
        }

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            _basePoint);

        if (!UpdateCurrentDestination(
                context,
                point))
        {
            return ToolResult.None("Scale factor must be greater than zero.");
        }

        return ToolResult.Updated(
            $"Scale: {_currentFactor:0.###}");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Scale command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Scale tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!HasPreview || _basePoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        Matrix2D matrix = Matrix2D.Scale(
            _currentFactor,
            _basePoint.Value);

        return context.Document.Entities
            .GetByIds(context.Selection.SelectedIds)
            .Select(entity => entity.Transform(matrix))
            .ToList();
    }

    private ToolResult AcceptBasePoint(
        ToolContext context,
        Point2D point)
    {
        _basePoint = point;
        _referencePoint = null;
        _currentDestinationPoint = null;
        _currentFactor = 1.0;
        State = ScaleToolState.WaitingForReferencePoint;
        context.CurrentBasePoint = point;

        return ToolResult.Started(
            "Specify reference point for scaling.");
    }

    private ToolResult AcceptReferencePoint(
        ToolContext context,
        Point2D point)
    {
        if (_basePoint is null)
        {
            throw new InvalidOperationException(
                "Cannot accept reference point before base point.");
        }

        if (AreSamePoint(
                _basePoint.Value,
                point,
                context))
        {
            return ToolResult.None(
                "Reference point must be different from base point.");
        }

        _referencePoint = point;
        _currentDestinationPoint = point;
        _currentFactor = 1.0;
        State = ScaleToolState.WaitingForDestinationPoint;
        context.CurrentBasePoint = _basePoint.Value;

        return ToolResult.Started(
            "Specify destination point for scaling.");
    }

    private ToolResult AcceptDestinationPoint(
        ToolContext context,
        Point2D point)
    {
        if (_basePoint is null || _referencePoint is null)
        {
            throw new InvalidOperationException(
                "Cannot accept destination point before base and reference points.");
        }

        if (!UpdateCurrentDestination(
                context,
                point))
        {
            return ToolResult.None(
                "Scale factor must be greater than zero.");
        }

        IReadOnlyList<EntityId> selectedIds = context.Selection.SelectedIds.ToList();

        context.Commands.Execute(
            context.Document,
            new ScaleEntitiesCommand(
                selectedIds,
                _basePoint.Value,
                _currentFactor));

        double committedFactor = _currentFactor;

        Reset(context);

        return ToolResult.Completed(
            $"Entities scaled by {committedFactor:0.###}.");
    }

    private bool UpdateCurrentDestination(
        ToolContext context,
        Point2D destinationPoint)
    {
        if (_basePoint is null || _referencePoint is null)
        {
            return false;
        }

        double referenceDistance = _basePoint.Value.DistanceTo(
            _referencePoint.Value);

        if (Tolerance.IsZero(referenceDistance))
        {
            return false;
        }

        double destinationDistance = _basePoint.Value.DistanceTo(
            destinationPoint);

        if (Tolerance.IsZero(destinationDistance))
        {
            return false;
        }

        _currentDestinationPoint = destinationPoint;
        _currentFactor = destinationDistance / referenceDistance;

        return _currentFactor > 0;
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
        return context.GeometryTolerance.ArePointsEqual(
            first,
            second);
    }

    private void Reset(ToolContext? context = null)
    {
        _basePoint = null;
        _referencePoint = null;
        _currentDestinationPoint = null;
        _currentFactor = 1.0;
        State = ScaleToolState.WaitingForBasePoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
