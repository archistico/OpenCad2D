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
/// Interactive tool used to copy selected entities.
/// If no entity is selected when the tool starts, the first phase lets the user select entities to copy.
/// </summary>
public sealed class CopyTool : ICadTool, ISnapModeProvider, ICommandDrivenTool, IKeyboardAwareTool
{
    private Point2D? _basePoint;
    private Point2D? _currentPoint;
    private MoveToolState _state;

    public CopyTool()
    {
        _state = MoveToolState.WaitingForBasePoint;
    }

    public string Name => "Copy";

    public MoveToolState CopyState => _state;

    /// <summary>
    /// Compatibility state for existing UI/tests that only need to know whether the tool is waiting
    /// for the first or second movement point.
    /// </summary>
    public TwoPointToolState State => _state == MoveToolState.WaitingForDestinationPoint
        ? TwoPointToolState.WaitingForSecondPoint
        : TwoPointToolState.WaitingForFirstPoint;

    public Point2D? FirstPoint => _basePoint;

    public Point2D? CurrentPoint => _currentPoint;

    public bool HasPreview =>
        _basePoint.HasValue &&
        _currentPoint.HasValue &&
        _state == MoveToolState.WaitingForDestinationPoint;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        return _state switch
        {
            MoveToolState.WaitingForEntitySelection => new CommandPromptState(
                "COPY",
                "Select objects to copy",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Click objects, then press Enter"),

            MoveToolState.WaitingForBasePoint => new CommandPromptState(
                "COPY",
                "Specify base point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            MoveToolState.WaitingForDestinationPoint => new CommandPromptState(
                "COPY",
                "Specify destination point",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   @100<45   |   distance"),

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

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return ConfirmEntitySelection(context);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "COPY expects a point input.");
        }

        return _state switch
        {
            MoveToolState.WaitingForBasePoint => AcceptBasePoint(context, input.Point.Value),
            MoveToolState.WaitingForDestinationPoint => AcceptDestinationPoint(context, input.Point.Value),
            MoveToolState.WaitingForEntitySelection => ToolResult.None("Select entities to copy, then press Enter."),
            _ => ToolResult.None()
        };
    }

    public bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (_state == MoveToolState.WaitingForEntitySelection &&
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

        return _state == MoveToolState.WaitingForEntitySelection
            ? SnapKind.EntityOnly
            : context.EnabledSnaps;
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!HasPreview || _basePoint is null || _currentPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        Vector2D displacement = _basePoint.Value.VectorTo(_currentPoint.Value);

        Matrix2D matrix = Matrix2D.Translation(
            displacement.X,
            displacement.Y);

        return context.Document.Entities
            .GetByIds(context.Selection.SelectedIds)
            .Select(entity => entity.Transform(matrix).WithId(EntityId.New()))
            .ToList();
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        EnsureInitialState(context);

        return _state switch
        {
            MoveToolState.WaitingForEntitySelection => SelectEntityToCopy(context, pointer),
            MoveToolState.WaitingForBasePoint => SelectBasePoint(context, pointer),
            MoveToolState.WaitingForDestinationPoint => SelectDestinationPoint(context, pointer),
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

        if (_state != MoveToolState.WaitingForDestinationPoint || _basePoint is null)
        {
            return ToolResult.None();
        }

        Point2D point = ApplyGeometricSnap(
            context,
            pointer.ModelPoint,
            _basePoint);

        _currentPoint = ToolInputConstraintService.ApplyAngleConstraint(
            context,
            _basePoint.Value,
            point);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Copy command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Copy tool deactivated.");
    }

    public ToolResult ConfirmEntitySelection(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (_state != MoveToolState.WaitingForEntitySelection)
        {
            return ToolResult.None();
        }

        if (!context.Selection.HasSelection)
        {
            return ToolResult.None("Select entities to copy.");
        }

        _state = MoveToolState.WaitingForBasePoint;

        return ToolResult.Started("Specify base point.");
    }

    private ToolResult SelectEntityToCopy(
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
            return ToolResult.None("Select entities to copy, then press Enter.");
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
            ? "Overlapping entity selected. Press Enter to specify base point."
            : "Entity selected. Press Enter to specify base point.");
    }

    private ToolResult SelectBasePoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ApplyGeometricSnap(
            context,
            pointer.ModelPoint,
            null);

        return AcceptBasePoint(context, point);
    }

    private ToolResult AcceptBasePoint(
        ToolContext context,
        Point2D point)
    {
        if (!context.Selection.HasSelection)
        {
            _state = MoveToolState.WaitingForEntitySelection;

            return ToolResult.Started("Select entities to copy.");
        }

        _basePoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = point;
        _state = MoveToolState.WaitingForDestinationPoint;

        return ToolResult.Started("Specify destination point.");
    }

    private ToolResult SelectDestinationPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_basePoint is null)
        {
            throw new InvalidOperationException(
                "Copy tool is waiting for destination point but base point is missing.");
        }

        Point2D point = ApplyGeometricSnap(
            context,
            pointer.ModelPoint,
            _basePoint);

        point = ToolInputConstraintService.ApplyAngleConstraint(
            context,
            _basePoint.Value,
            point);

        return AcceptDestinationPoint(context, point);
    }

    private ToolResult AcceptDestinationPoint(
        ToolContext context,
        Point2D point)
    {
        if (!context.Selection.HasSelection)
        {
            Reset(context);

            return ToolResult.None("No entities selected.");
        }

        if (_basePoint is null)
        {
            throw new InvalidOperationException(
                "Copy tool is waiting for destination point but base point is missing.");
        }

        if (context.GeometryTolerance.ArePointsEqual(_basePoint.Value, point))
        {
            return ToolResult.None("Destination point must be different from base point.");
        }

        Vector2D displacement = _basePoint.Value.VectorTo(point);

        IReadOnlyList<EntityId> selectedIds =
            context.Selection.SelectedIds.ToList();

        context.Commands.Execute(
            context.Document,
            new CopyEntitiesCommand(
                selectedIds,
                displacement));

        Reset(context);

        return ToolResult.Completed("Entities copied.");
    }

    private Point2D ApplyGeometricSnap(
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

    private void EnsureInitialState(ToolContext context)
    {
        if (_basePoint is not null)
        {
            return;
        }

        if (_state == MoveToolState.WaitingForEntitySelection &&
            context.Selection.HasSelection)
        {
            return;
        }

        _state = context.Selection.HasSelection
            ? MoveToolState.WaitingForBasePoint
            : MoveToolState.WaitingForEntitySelection;
    }

    private void Reset(ToolContext context)
    {
        _basePoint = null;
        _currentPoint = null;
        context.CurrentBasePoint = null;

        _state = context.Selection.HasSelection
            ? MoveToolState.WaitingForBasePoint
            : MoveToolState.WaitingForEntitySelection;
    }
}
