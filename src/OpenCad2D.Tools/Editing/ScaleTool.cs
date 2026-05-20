using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Interactive tool used to uniformly scale the current selection around a base point.
/// </summary>
public sealed class ScaleTool : ICadTool, ISnapModeProvider, ICommandDrivenTool, IKeyboardAwareTool, IToolPreviewEntityProvider
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


    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        return State switch
        {
            ScaleToolState.WaitingForEntitySelection => new CommandPromptState(
                "SCALE",
                "Select objects to scale",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Click objects, then press Enter/right-click"),

            ScaleToolState.WaitingForBasePoint => new CommandPromptState(
                "SCALE",
                "Specify base point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            ScaleToolState.WaitingForReferencePoint => new CommandPromptState(
                "SCALE",
                "Specify reference point",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            ScaleToolState.WaitingForDestinationPoint => new CommandPromptState(
                "SCALE",
                "Specify destination point or type scale factor",
                CommandInputKind.PointOrNumber,
                placeholder: "point or factor, e.g. @100,0 or 2"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (State == ScaleToolState.WaitingForEntitySelection)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return ConfirmEntitySelection(context);
            }

            return ToolResult.None("Select entities to scale, then press Enter/right-click.");
        }

        if (State == ScaleToolState.WaitingForDestinationPoint &&
            input.Kind == CommandInputSubmissionKind.Number &&
            input.Number is not null)
        {
            return AcceptScaleFactor(context, input.Number.Value);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "SCALE expects a point or scale factor input.");
        }

        return State switch
        {
            ScaleToolState.WaitingForBasePoint => AcceptBasePoint(context, input.Point.Value),
            ScaleToolState.WaitingForReferencePoint => AcceptReferencePoint(context, input.Point.Value),
            ScaleToolState.WaitingForDestinationPoint => AcceptDestinationPoint(context, input.Point.Value),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        EnsureInitialState(context);

        if (State == ScaleToolState.WaitingForEntitySelection)
        {
            return SelectEntityToScale(context, pointer);
        }

        Point2D point = ApplyGeometricSnap(
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

        EnsureInitialState(context);

        if (State != ScaleToolState.WaitingForDestinationPoint ||
            _basePoint is null ||
            _referencePoint is null)
        {
            return ToolResult.None();
        }

        Point2D point = ApplyGeometricSnap(
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

    public bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (State == ScaleToolState.WaitingForEntitySelection &&
            key == CadToolKey.Enter)
        {
            result = ConfirmEntitySelection(context);
            return result.Changed;
        }

        result = ToolResult.None();
        return false;
    }

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        return State == ScaleToolState.WaitingForEntitySelection
            ? SnapKind.EntityOnly
            : context.EnabledSnaps;
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
        if (!context.Selection.HasSelection)
        {
            State = ScaleToolState.WaitingForEntitySelection;

            return ToolResult.Started("Select entities to scale.");
        }

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


    private ToolResult AcceptScaleFactor(
        ToolContext context,
        double factor)
    {
        if (_basePoint is null || _referencePoint is null)
        {
            return ToolResult.None("Specify base and reference points before typing a scale factor.");
        }

        if (factor <= 0 || Tolerance.IsZero(factor))
        {
            return ToolResult.None("Scale factor must be greater than zero.");
        }

        IReadOnlyList<EntityId> selectedIds = context.Selection.SelectedIds.ToList();

        context.Commands.Execute(
            context.Document,
            new ScaleEntitiesCommand(
                selectedIds,
                _basePoint.Value,
                factor));

        Reset(context);

        return ToolResult.Completed(
            $"Entities scaled by {factor:0.###}.");
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

    public ToolResult ConfirmEntitySelection(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (State != ScaleToolState.WaitingForEntitySelection)
        {
            return ToolResult.None();
        }

        if (!context.Selection.HasSelection)
        {
            return ToolResult.None("Select entities to scale.");
        }

        State = ScaleToolState.WaitingForBasePoint;

        return ToolResult.Started("Specify base point for scaling.");
    }

    private ToolResult SelectEntityToScale(
        ToolContext context,
        PointerInfo pointer)
    {
        EntityId? selectedId = pointer.IsControlPressed
            ? context.Selection.Service.SelectNextByPoint(
                context.Document,
                pointer.ModelPoint,
                context.Selection.Tolerance,
                context.Selection.Set.LastSelectedId)
            : context.Selection.Service.SelectByPoint(
                context.Document,
                pointer.ModelPoint,
                context.Selection.Tolerance);

        if (selectedId is null)
        {
            return ToolResult.None("Select entities to scale, then press Enter/right-click.");
        }

        if (pointer.IsShiftPressed)
        {
            context.Selection.Set.Toggle(selectedId.Value);
        }
        else
        {
            context.Selection.Set.ReplaceWith(selectedId.Value);
        }

        return ToolResult.Updated(pointer.IsControlPressed
            ? "Overlapping entity selected. Press Enter/right-click to specify base point."
            : "Entity selected. Press Enter/right-click to specify base point.");
    }

    private static Point2D ApplyGeometricSnap(
        ToolContext context,
        Point2D cursorPoint,
        Point2D? basePoint)
    {
        SnapKind enabledSnaps = context.EnabledSnaps & ~SnapKind.Entity;

        if (enabledSnaps == SnapKind.None ||
            Tolerance.IsZero(context.SnapTolerance))
        {
            return cursorPoint;
        }

        var request = new SnapRequest(
            context.Document,
            cursorPoint,
            context.SnapTolerance,
            enabledSnaps,
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

    private void EnsureInitialState(ToolContext context)
    {
        if (_basePoint is not null)
        {
            return;
        }

        if (State == ScaleToolState.WaitingForEntitySelection &&
            context.Selection.HasSelection)
        {
            return;
        }

        State = context.Selection.HasSelection
            ? ScaleToolState.WaitingForBasePoint
            : ScaleToolState.WaitingForEntitySelection;
    }

    private void Reset(ToolContext? context = null)
    {
        _basePoint = null;
        _referencePoint = null;
        _currentDestinationPoint = null;
        _currentFactor = 1.0;
        State = context is not null && !context.Selection.HasSelection
            ? ScaleToolState.WaitingForEntitySelection
            : ScaleToolState.WaitingForBasePoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
