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
/// Interactive tool used to rotate the current selection around a base point.
/// </summary>
public sealed class RotateTool : ICadTool, ISnapModeProvider, ICommandDrivenTool, IKeyboardAwareTool, IToolPreviewEntityProvider
{
    private Point2D? _basePoint;
    private Point2D? _referencePoint;
    private Point2D? _currentDestinationPoint;
    private Angle _currentAngle = Angle.Zero;

    public string Name => "Rotate";

    public RotateToolState State { get; private set; } =
        RotateToolState.WaitingForBasePoint;

    public Point2D? BasePoint => _basePoint;

    public Point2D? ReferencePoint => _referencePoint;

    public Point2D? CurrentDestinationPoint => _currentDestinationPoint;

    public Angle CurrentAngle => _currentAngle;

    public bool HasPreview =>
        State == RotateToolState.WaitingForDestinationPoint &&
        _basePoint.HasValue &&
        _referencePoint.HasValue &&
        _currentDestinationPoint.HasValue;


    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        return State switch
        {
            RotateToolState.WaitingForEntitySelection => new CommandPromptState(
                "ROTATE",
                "Select objects to rotate",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Click objects, then press Enter/right-click"),

            RotateToolState.WaitingForBasePoint => new CommandPromptState(
                "ROTATE",
                "Specify base point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            RotateToolState.WaitingForReferencePoint => new CommandPromptState(
                "ROTATE",
                "Specify reference point",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            RotateToolState.WaitingForDestinationPoint => new CommandPromptState(
                "ROTATE",
                "Specify destination point or type angle",
                CommandInputKind.PointOrAngle,
                placeholder: "point or angle, e.g. @100<45 or 90"),

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

        if (State == RotateToolState.WaitingForEntitySelection)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return ConfirmEntitySelection(context);
            }

            return ToolResult.None("Select entities to rotate, then press Enter/right-click.");
        }

        if (State == RotateToolState.WaitingForDestinationPoint &&
            input.Kind == CommandInputSubmissionKind.Angle &&
            input.AngleDegrees is not null)
        {
            return AcceptAngle(context, input.AngleDegrees.Value);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "ROTATE expects a point or angle input.");
        }

        return State switch
        {
            RotateToolState.WaitingForBasePoint => AcceptBasePoint(context, input.Point.Value),
            RotateToolState.WaitingForReferencePoint => AcceptReferencePoint(context, input.Point.Value),
            RotateToolState.WaitingForDestinationPoint => AcceptDestinationPoint(context, input.Point.Value),
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

        if (State == RotateToolState.WaitingForEntitySelection)
        {
            return SelectEntityToRotate(context, pointer);
        }

        Point2D point = ApplyGeometricSnap(
            context,
            pointer.ModelPoint,
            _basePoint);

        return State switch
        {
            RotateToolState.WaitingForBasePoint =>
                AcceptBasePoint(context, point),

            RotateToolState.WaitingForReferencePoint =>
                AcceptReferencePoint(context, point),

            RotateToolState.WaitingForDestinationPoint =>
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

        if (State != RotateToolState.WaitingForDestinationPoint ||
            _basePoint is null ||
            _referencePoint is null)
        {
            return ToolResult.None();
        }

        Point2D point = ApplyGeometricSnap(
            context,
            pointer.ModelPoint,
            _basePoint);

        UpdateCurrentDestination(
            context,
            point);

        return ToolResult.Updated(
            $"Angle: {_currentAngle.Degrees:0.##}°");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Rotate command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Rotate tool deactivated.");
    }

    public bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (State == RotateToolState.WaitingForEntitySelection &&
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

        return State == RotateToolState.WaitingForEntitySelection
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

        Matrix2D matrix = Matrix2D.Rotation(
            _currentAngle.Radians,
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
            State = RotateToolState.WaitingForEntitySelection;

            return ToolResult.Started("Select entities to rotate.");
        }

        _basePoint = point;
        _referencePoint = null;
        _currentDestinationPoint = null;
        _currentAngle = Angle.Zero;
        State = RotateToolState.WaitingForReferencePoint;
        context.CurrentBasePoint = point;

        return ToolResult.Started(
            "Specify reference point for rotation.");
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
        _currentAngle = Angle.Zero;
        State = RotateToolState.WaitingForDestinationPoint;
        context.CurrentBasePoint = _basePoint.Value;

        return ToolResult.Started(
            "Specify destination point for rotation.");
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

        UpdateCurrentDestination(
            context,
            point);

        IReadOnlyList<EntityId> selectedIds = context.Selection.SelectedIds.ToList();

        context.Commands.Execute(
            context.Document,
            new RotateEntitiesCommand(
                selectedIds,
                _basePoint.Value,
                _currentAngle));

        Reset(context);

        return ToolResult.Completed(
            $"Entities rotated by {_currentAngle.Degrees:0.##}°.");
    }


    private ToolResult AcceptAngle(
        ToolContext context,
        double angleDegrees)
    {
        if (_basePoint is null || _referencePoint is null)
        {
            return ToolResult.None("Specify base and reference points before typing an angle.");
        }

        IReadOnlyList<EntityId> selectedIds = context.Selection.SelectedIds.ToList();
        Angle angle = Angle.FromDegrees(angleDegrees);

        context.Commands.Execute(
            context.Document,
            new RotateEntitiesCommand(
                selectedIds,
                _basePoint.Value,
                angle));

        Reset(context);

        return ToolResult.Completed(
            $"Entities rotated by {angle.Degrees:0.##}°.");
    }

    private void UpdateCurrentDestination(
        ToolContext context,
        Point2D destinationPoint)
    {
        if (_basePoint is null || _referencePoint is null)
        {
            return;
        }

        double angleRadians = CalculateAngle(
            _basePoint.Value,
            _referencePoint.Value,
            destinationPoint);

        if (context.IsOrthoEnabled)
        {
            angleRadians = SnapAngleToRightAngle(angleRadians);
            destinationPoint = RebuildDestinationPointFromAngle(
                _basePoint.Value,
                _referencePoint.Value,
                angleRadians);
        }

        _currentDestinationPoint = destinationPoint;
        _currentAngle = Angle.FromRadians(angleRadians);
    }

    private static double CalculateAngle(
        Point2D basePoint,
        Point2D referencePoint,
        Point2D destinationPoint)
    {
        Vector2D referenceVector = basePoint.VectorTo(referencePoint);
        Vector2D destinationVector = basePoint.VectorTo(destinationPoint);

        if (Tolerance.IsZero(referenceVector.Length) ||
            Tolerance.IsZero(destinationVector.Length))
        {
            return 0;
        }

        double referenceAngle = Math.Atan2(
            referenceVector.Y,
            referenceVector.X);

        double destinationAngle = Math.Atan2(
            destinationVector.Y,
            destinationVector.X);

        return destinationAngle - referenceAngle;
    }

    private static double SnapAngleToRightAngle(double angleRadians)
    {
        double step = Math.PI / 2.0;

        return Math.Round(angleRadians / step) * step;
    }

    private static Point2D RebuildDestinationPointFromAngle(
        Point2D basePoint,
        Point2D referencePoint,
        double angleRadians)
    {
        Vector2D referenceVector = basePoint.VectorTo(referencePoint);
        double length = referenceVector.Length;
        double referenceAngle = Math.Atan2(
            referenceVector.Y,
            referenceVector.X);
        double destinationAngle = referenceAngle + angleRadians;

        return new Point2D(
            basePoint.X + Math.Cos(destinationAngle) * length,
            basePoint.Y + Math.Sin(destinationAngle) * length);
    }

    public ToolResult ConfirmEntitySelection(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (State != RotateToolState.WaitingForEntitySelection)
        {
            return ToolResult.None();
        }

        if (!context.Selection.HasSelection)
        {
            return ToolResult.None("Select entities to rotate.");
        }

        State = RotateToolState.WaitingForBasePoint;

        return ToolResult.Started("Specify base point for rotation.");
    }

    private ToolResult SelectEntityToRotate(
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
            return ToolResult.None("Select entities to rotate, then press Enter/right-click.");
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

        if (State == RotateToolState.WaitingForEntitySelection &&
            context.Selection.HasSelection)
        {
            return;
        }

        State = context.Selection.HasSelection
            ? RotateToolState.WaitingForBasePoint
            : RotateToolState.WaitingForEntitySelection;
    }

    private void Reset(ToolContext? context = null)
    {
        _basePoint = null;
        _referencePoint = null;
        _currentDestinationPoint = null;
        _currentAngle = Angle.Zero;
        State = context is not null && !context.Selection.HasSelection
            ? RotateToolState.WaitingForEntitySelection
            : RotateToolState.WaitingForBasePoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
