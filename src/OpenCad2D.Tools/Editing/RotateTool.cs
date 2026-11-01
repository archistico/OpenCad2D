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
/// Interactive tool used to rotate the current selection around a base point, following the
/// AutoCAD ROTATE workflow:
/// <code>
/// [select objects] -> base point -> rotation angle or [Copy/Reference] &lt;last&gt;
/// Reference: reference angle (typed or two points) -> new angle or [Points]
/// </code>
/// Angles are counter-clockwise degrees measured from the X axis of the current UCS. A clicked
/// point defines the angle of the direction from the base point (or from the first point of a pair).
/// </summary>
public sealed class RotateTool : ICadTool, ISnapModeProvider, ICommandDrivenTool, IKeyboardAwareTool, IToolPreviewEntityProvider
{
    private const double RotationAngleTolerance = 1e-12;

    private static double s_lastRotationDegrees;
    private static double s_lastReferenceDegrees;

    private Point2D? _basePoint;
    private Point2D? _pairFirstPoint;
    private Point2D? _currentDestinationPoint;
    private double? _referenceRadians;
    private bool _isCopyMode;
    private int _skippedCount;
    private Angle _currentAngle = Angle.Zero;

    public string Name => "Rotate";

    public RotateToolState State { get; private set; } =
        RotateToolState.WaitingForBasePoint;

    public bool IsCommandFinished { get; private set; }

    public Point2D? BasePoint => _basePoint;

    /// <summary>
    /// First point of a two-point reference or new-angle definition while its second point is pending.
    /// </summary>
    public Point2D? ReferencePoint => _pairFirstPoint;

    public Point2D? CurrentDestinationPoint => _currentDestinationPoint;

    /// <summary>
    /// Origin of the live guide vector: the first point of a pending point pair, otherwise the base point.
    /// </summary>
    public Point2D? GuideOrigin => _pairFirstPoint ?? _basePoint;

    public Angle CurrentAngle => _currentAngle;

    public double? ReferenceAngleDegrees => _referenceRadians is null
        ? null
        : _referenceRadians.Value * 180.0 / Math.PI;

    public bool IsCopyMode => _isCopyMode;

    public static double LastRotationDegrees => s_lastRotationDegrees;

    public static double LastReferenceDegrees => s_lastReferenceDegrees;

    public bool HasPreview =>
        (State is RotateToolState.WaitingForRotationAngle or RotateToolState.WaitingForNewAngle) &&
        _basePoint.HasValue &&
        _currentDestinationPoint.HasValue;

    public static void ResetDefaultsForTests()
    {
        s_lastRotationDegrees = 0;
        s_lastReferenceDegrees = 0;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        string copySuffix = _isCopyMode ? " (copy)" : string.Empty;

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
                "Specify base point or press Enter to finish",
                CommandInputKind.Point,
                acceptsEmptyEnter: true,
                placeholder: "100,50   |   Enter finishes"),

            RotateToolState.WaitingForRotationAngle => new CommandPromptState(
                "ROTATE",
                $"Specify rotation angle{copySuffix} <{FormatDegrees(s_lastRotationDegrees)}>",
                CommandInputKind.PointOrAngleOrOption,
                new[]
                {
                    new CommandOption("Copy", "C", "Rotate a copy and keep the original objects"),
                    new CommandOption("Reference", "R", "Rotate from a reference angle to a new angle")
                },
                acceptsEmptyEnter: true,
                placeholder: "angle, e.g. 90 or -45, or click a direction; C copy; R reference"),

            RotateToolState.WaitingForReferenceAngle => new CommandPromptState(
                "ROTATE",
                $"Specify the reference angle{copySuffix} <{FormatDegrees(s_lastReferenceDegrees)}>",
                CommandInputKind.PointOrAngle,
                acceptsEmptyEnter: true,
                placeholder: "reference angle, or click the first point of the reference line"),

            RotateToolState.WaitingForReferenceSecondPoint => new CommandPromptState(
                "ROTATE",
                "Specify second point of the reference line",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            RotateToolState.WaitingForNewAngle => new CommandPromptState(
                "ROTATE",
                $"Specify the new angle{copySuffix} <{FormatDegrees(ReferenceAngleDegrees ?? 0)}>",
                CommandInputKind.PointOrAngleOrOption,
                new[]
                {
                    new CommandOption("Points", "P", "Define the new angle with two points")
                },
                acceptsEmptyEnter: true,
                placeholder: "new angle, or click a direction from the base point; P two points"),

            RotateToolState.WaitingForNewAngleFirstPoint => new CommandPromptState(
                "ROTATE",
                "Specify first point of the new angle",
                CommandInputKind.Point,
                placeholder: "100,50"),

            RotateToolState.WaitingForNewAngleSecondPoint => new CommandPromptState(
                "ROTATE",
                "Specify second point of the new angle",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

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

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return State switch
            {
                RotateToolState.WaitingForBasePoint => FinishCommand(context),
                RotateToolState.WaitingForRotationAngle => ExecuteRotation(context, Angle.FromDegrees(s_lastRotationDegrees)),
                RotateToolState.WaitingForReferenceAngle => AcceptReferenceAngle(context, DegreesToRadians(s_lastReferenceDegrees)),
                RotateToolState.WaitingForNewAngle => AcceptNewAngle(context, _referenceRadians ?? 0),
                _ => ToolResult.None("Specify the requested point.")
            };
        }

        if (input.Kind == CommandInputSubmissionKind.Option && input.OptionKeyword is not null)
        {
            return AcceptOption(context, input.OptionKeyword);
        }

        if (input.Kind == CommandInputSubmissionKind.Angle && input.AngleDegrees is not null)
        {
            double radians = DegreesToRadians(input.AngleDegrees.Value);

            return State switch
            {
                RotateToolState.WaitingForRotationAngle => ExecuteRotation(context, Angle.FromRadians(radians)),
                RotateToolState.WaitingForReferenceAngle => AcceptReferenceAngle(context, radians),
                RotateToolState.WaitingForNewAngle => AcceptNewAngle(context, radians),
                _ => ToolResult.None("Specify the requested point.")
            };
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "ROTATE expects a point or angle input.");
        }

        // Typed coordinates are explicit and stay exact: no snap, ortho or polar adjustment.
        return AcceptPoint(context, input.Point.Value);
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

        return AcceptPoint(
            context,
            ResolvePointerPoint(context, pointer.ModelPoint));
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        EnsureInitialState(context);

        if (_basePoint is null || !IsDirectionState(State))
        {
            return ToolResult.None();
        }

        Point2D point = ResolvePointerPoint(context, pointer.ModelPoint);
        _currentDestinationPoint = point;

        if (State == RotateToolState.WaitingForRotationAngle)
        {
            _currentAngle = Angle.FromRadians(GetUserAngle(context, _basePoint.Value, point) ?? 0);
            return ToolResult.Updated($"Angle: {FormatDegrees(_currentAngle.Degrees)}°");
        }

        if (State == RotateToolState.WaitingForNewAngle)
        {
            double newRadians = GetUserAngle(context, _basePoint.Value, point) ?? (_referenceRadians ?? 0);
            _currentAngle = Angle.FromRadians(newRadians - (_referenceRadians ?? 0));
            return ToolResult.Updated($"Rotation: {FormatDegrees(_currentAngle.Degrees)}°");
        }

        return ToolResult.Updated("Point preview updated.");
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
            : context.EnabledSnaps & ~SnapKind.Entity;
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

        return EditableSelection.GetEntities(context)
            .Select(entity => entity.Transform(matrix))
            .ToList();
    }

    public ToolResult ConfirmEntitySelection(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (State != RotateToolState.WaitingForEntitySelection)
        {
            return ToolResult.None();
        }

        int skipped = EditableSelection.Prune(context);
        if (!context.Selection.HasSelection)
        {
            return ToolResult.None(skipped > 0
                ? "The selected entities were deleted or are on hidden or locked layers. Select entities to rotate."
                : "Select entities to rotate.");
        }

        _skippedCount += skipped;
        State = RotateToolState.WaitingForBasePoint;

        return ToolResult.Started("Specify base point for rotation.");
    }

    private static bool IsDirectionState(RotateToolState state)
    {
        return state is
            RotateToolState.WaitingForRotationAngle or
            RotateToolState.WaitingForReferenceSecondPoint or
            RotateToolState.WaitingForNewAngle or
            RotateToolState.WaitingForNewAngleSecondPoint;
    }

    private ToolResult AcceptPoint(
        ToolContext context,
        Point2D point)
    {
        return State switch
        {
            RotateToolState.WaitingForBasePoint => AcceptBasePoint(context, point),
            RotateToolState.WaitingForRotationAngle => AcceptRotationDirection(context, point),
            RotateToolState.WaitingForReferenceAngle => AcceptPairFirstPoint(
                context,
                point,
                RotateToolState.WaitingForReferenceSecondPoint,
                "Specify second point of the reference line."),
            RotateToolState.WaitingForReferenceSecondPoint => AcceptReferenceSecondPoint(context, point),
            RotateToolState.WaitingForNewAngle => AcceptNewAngleDirection(context, point),
            RotateToolState.WaitingForNewAngleFirstPoint => AcceptPairFirstPoint(
                context,
                point,
                RotateToolState.WaitingForNewAngleSecondPoint,
                "Specify second point of the new angle."),
            RotateToolState.WaitingForNewAngleSecondPoint => AcceptNewAngleSecondPoint(context, point),
            _ => ToolResult.None()
        };
    }

    private ToolResult AcceptBasePoint(
        ToolContext context,
        Point2D point)
    {
        int skipped = EditableSelection.Prune(context);
        if (!context.Selection.HasSelection)
        {
            State = RotateToolState.WaitingForEntitySelection;

            return ToolResult.Started(skipped > 0
                ? "The selected entities were deleted or are on hidden or locked layers. Select entities to rotate."
                : "Select entities to rotate.");
        }

        _skippedCount += skipped;
        _basePoint = point;
        _pairFirstPoint = null;
        _currentDestinationPoint = null;
        _referenceRadians = null;
        _currentAngle = Angle.Zero;
        State = RotateToolState.WaitingForRotationAngle;
        context.CurrentBasePoint = point;

        return ToolResult.Started(
            $"Specify rotation angle or [Copy/Reference] <{FormatDegrees(s_lastRotationDegrees)}>.");
    }

    private ToolResult AcceptOption(
        ToolContext context,
        string keyword)
    {
        if (State == RotateToolState.WaitingForRotationAngle &&
            string.Equals(keyword, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            _isCopyMode = !_isCopyMode;

            return ToolResult.Started(_isCopyMode
                ? "Rotating a copy of the selected objects. Specify rotation angle."
                : "Rotating the selected objects. Specify rotation angle.");
        }

        if (State == RotateToolState.WaitingForRotationAngle &&
            string.Equals(keyword, "Reference", StringComparison.OrdinalIgnoreCase))
        {
            _currentDestinationPoint = null;
            _currentAngle = Angle.Zero;
            State = RotateToolState.WaitingForReferenceAngle;

            return ToolResult.Started(
                $"Specify the reference angle <{FormatDegrees(s_lastReferenceDegrees)}>, or click the first point of the reference line.");
        }

        if (State == RotateToolState.WaitingForNewAngle &&
            string.Equals(keyword, "Points", StringComparison.OrdinalIgnoreCase))
        {
            _currentDestinationPoint = null;
            _currentAngle = Angle.Zero;
            State = RotateToolState.WaitingForNewAngleFirstPoint;

            return ToolResult.Started("Specify first point of the new angle.");
        }

        return ToolResult.None($"Option {keyword} is not available now.");
    }

    private ToolResult AcceptRotationDirection(
        ToolContext context,
        Point2D point)
    {
        double? radians = GetUserAngle(context, _basePoint!.Value, point);
        if (radians is null)
        {
            return ToolResult.None(
                "The direction point must be different from the base point.");
        }

        return ExecuteRotation(
            context,
            Angle.FromRadians(radians.Value));
    }

    private ToolResult AcceptPairFirstPoint(
        ToolContext context,
        Point2D point,
        RotateToolState nextState,
        string message)
    {
        _pairFirstPoint = point;
        _currentDestinationPoint = null;
        context.CurrentBasePoint = point;
        State = nextState;

        return ToolResult.Started(message);
    }

    private ToolResult AcceptReferenceSecondPoint(
        ToolContext context,
        Point2D point)
    {
        double? radians = GetUserAngle(context, _pairFirstPoint!.Value, point);
        if (radians is null)
        {
            return ToolResult.None(
                "The second reference point must be different from the first one.");
        }

        return AcceptReferenceAngle(context, radians.Value);
    }

    private ToolResult AcceptReferenceAngle(
        ToolContext context,
        double radians)
    {
        _referenceRadians = radians;
        s_lastReferenceDegrees = radians * 180.0 / Math.PI;
        _pairFirstPoint = null;
        _currentDestinationPoint = null;
        _currentAngle = Angle.Zero;
        context.CurrentBasePoint = _basePoint;
        State = RotateToolState.WaitingForNewAngle;

        return ToolResult.Started(
            $"Reference angle {FormatDegrees(s_lastReferenceDegrees)}°. Specify the new angle or [Points].");
    }

    private ToolResult AcceptNewAngleDirection(
        ToolContext context,
        Point2D point)
    {
        double? radians = GetUserAngle(context, _basePoint!.Value, point);
        if (radians is null)
        {
            return ToolResult.None(
                "The direction point must be different from the base point.");
        }

        return AcceptNewAngle(context, radians.Value);
    }

    private ToolResult AcceptNewAngleSecondPoint(
        ToolContext context,
        Point2D point)
    {
        double? radians = GetUserAngle(context, _pairFirstPoint!.Value, point);
        if (radians is null)
        {
            return ToolResult.None(
                "The second point must be different from the first one.");
        }

        return AcceptNewAngle(context, radians.Value);
    }

    private ToolResult AcceptNewAngle(
        ToolContext context,
        double newRadians)
    {
        ToolResult result = ExecuteRotation(
            context,
            Angle.FromRadians(newRadians - (_referenceRadians ?? 0)));

        if (result.Kind == ToolResultKind.None &&
            State == RotateToolState.WaitingForNewAngleSecondPoint)
        {
            // A two-point new angle equal to the reference angle: ask for the new angle again.
            _pairFirstPoint = null;
            _currentDestinationPoint = null;
            context.CurrentBasePoint = _basePoint;
            State = RotateToolState.WaitingForNewAngle;
        }

        return result;
    }

    private ToolResult ExecuteRotation(
        ToolContext context,
        Angle angle)
    {
        if (_basePoint is null)
        {
            return ToolResult.None("Specify the base point first.");
        }

        // A full turn or no turn leaves the geometry unchanged; executing it would only
        // add an empty undo step and mark the document as modified.
        double normalizedRadians = Math.IEEERemainder(angle.Radians, 2.0 * Math.PI);
        if (Math.Abs(normalizedRadians) <= RotationAngleTolerance)
        {
            return ToolResult.None(
                "Rotation angle is zero. Specify a different angle or direction.");
        }

        int skipped = _skippedCount + EditableSelection.Prune(context);
        IReadOnlyList<CadEntity> entities = EditableSelection.GetEntities(context);

        if (entities.Count == 0)
        {
            Reset(context);

            return ToolResult.None("No editable entities selected." + EditableSelection.FormatSkippedSuffix(skipped));
        }

        Angle appliedAngle = Angle.FromRadians(normalizedRadians);
        bool isCopy = _isCopyMode;

        if (isCopy)
        {
            Matrix2D matrix = Matrix2D.Rotation(appliedAngle.Radians, _basePoint.Value);
            context.Commands.Execute(
                context.Document,
                new AddEntityCommand(entities
                    .Select(entity => entity.Transform(matrix).WithId(EntityId.New()))
                    .ToList()));
        }
        else
        {
            context.Commands.Execute(
                context.Document,
                new RotateEntitiesCommand(
                    entities.Select(entity => entity.Id).ToList(),
                    _basePoint.Value,
                    appliedAngle));
        }

        s_lastRotationDegrees = appliedAngle.Degrees;

        Reset(context);

        return ToolResult.Completed(
            (isCopy ? "Rotated copy created" : "Entities rotated") +
            $" by {FormatDegrees(appliedAngle.Degrees)}°." +
            EditableSelection.FormatSkippedSuffix(skipped));
    }

    private Point2D ResolvePointerPoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        Point2D? origin = GuideOrigin;
        Point2D point = ApplyGeometricSnap(context, cursorPoint, origin);

        // Ortho and polar tracking only make sense while a direction is being defined.
        return origin is not null && IsDirectionState(State)
            ? ToolInputConstraintService.ApplyAngleConstraint(context, origin.Value, point)
            : point;
    }

    /// <summary>
    /// Returns the counter-clockwise angle of the direction from <paramref name="from"/> to
    /// <paramref name="to"/> in the current UCS, or null when the points coincide.
    /// </summary>
    private static double? GetUserAngle(
        ToolContext context,
        Point2D from,
        Point2D to)
    {
        if (context.GeometryTolerance.ArePointsEqual(from, to))
        {
            return null;
        }

        Vector2D direction = context.CurrentUcs.WorldVectorToUser(from.VectorTo(to));

        return Math.Atan2(direction.Y, direction.X);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static string FormatDegrees(double degrees)
    {
        return degrees.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
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

        State = EditableSelection.HasAny(context)
            ? RotateToolState.WaitingForBasePoint
            : RotateToolState.WaitingForEntitySelection;
    }

    private ToolResult FinishCommand(ToolContext context)
    {
        Reset(context);
        IsCommandFinished = true;

        return ToolResult.Completed("Rotate command finished.");
    }

    private void Reset(ToolContext? context = null)
    {
        _basePoint = null;
        _pairFirstPoint = null;
        _currentDestinationPoint = null;
        _referenceRadians = null;
        _isCopyMode = false;
        _currentAngle = Angle.Zero;
        _skippedCount = 0;
        IsCommandFinished = false;
        State = context is not null && !EditableSelection.HasAny(context)
            ? RotateToolState.WaitingForEntitySelection
            : RotateToolState.WaitingForBasePoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
