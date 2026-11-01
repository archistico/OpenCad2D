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
/// Interactive tool used to uniformly scale the current selection around a base point, following
/// the AutoCAD SCALE workflow:
/// <code>
/// [select objects] -> base point -> scale factor or [Copy/Reference] &lt;last&gt;
/// Reference: reference length (typed or two points) -> new length or [Points]
/// </code>
/// A clicked point in the scale-factor phase uses its distance from the base point, in drawing
/// units, as the factor; in the new-length phase it uses that distance as the new length.
/// </summary>
public sealed class ScaleTool : ICadTool, ISnapModeProvider, ICommandDrivenTool, IKeyboardAwareTool, IToolPreviewEntityProvider
{
    private static double s_lastScaleFactor = 1.0;
    private static double s_lastReferenceLength = 1.0;

    private Point2D? _basePoint;
    private Point2D? _pairFirstPoint;
    private Point2D? _currentDestinationPoint;
    private double? _referenceLength;
    private bool _isCopyMode;
    private int _skippedCount;
    private double _currentFactor = 1.0;

    public string Name => "Scale";

    public ScaleToolState State { get; private set; } =
        ScaleToolState.WaitingForBasePoint;

    public bool IsCommandFinished { get; private set; }

    public Point2D? BasePoint => _basePoint;

    /// <summary>
    /// First point of a two-point reference or new-length definition while its second point is pending.
    /// </summary>
    public Point2D? ReferencePoint => _pairFirstPoint;

    public Point2D? CurrentDestinationPoint => _currentDestinationPoint;

    /// <summary>
    /// Origin of the live guide vector: the first point of a pending point pair, otherwise the base point.
    /// </summary>
    public Point2D? GuideOrigin => _pairFirstPoint ?? _basePoint;

    public double CurrentFactor => _currentFactor;

    public double? ReferenceLength => _referenceLength;

    public bool IsCopyMode => _isCopyMode;

    public static double LastScaleFactor => s_lastScaleFactor;

    public static double LastReferenceLength => s_lastReferenceLength;

    public bool HasPreview =>
        (State is ScaleToolState.WaitingForScaleFactor or ScaleToolState.WaitingForNewLength) &&
        _basePoint.HasValue &&
        _currentDestinationPoint.HasValue &&
        _currentFactor > 0;

    public static void ResetDefaultsForTests()
    {
        s_lastScaleFactor = 1.0;
        s_lastReferenceLength = 1.0;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        string copySuffix = _isCopyMode ? " (copy)" : string.Empty;

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
                "Specify base point or press Enter to finish",
                CommandInputKind.Point,
                acceptsEmptyEnter: true,
                placeholder: "100,50   |   Enter finishes"),

            ScaleToolState.WaitingForScaleFactor => new CommandPromptState(
                "SCALE",
                $"Specify scale factor{copySuffix} <{FormatNumber(s_lastScaleFactor)}>",
                CommandInputKind.PointOrNumberOrOption,
                new[]
                {
                    new CommandOption("Copy", "C", "Scale a copy and keep the original objects"),
                    new CommandOption("Reference", "R", "Scale from a reference length to a new length")
                },
                acceptsEmptyEnter: true,
                placeholder: "factor, e.g. 2 or 0.5, or click a point; C copy; R reference"),

            ScaleToolState.WaitingForReferenceLength => new CommandPromptState(
                "SCALE",
                $"Specify reference length{copySuffix} <{FormatNumber(s_lastReferenceLength)}>",
                CommandInputKind.PointOrNumber,
                acceptsEmptyEnter: true,
                placeholder: "reference length, or click the first point of the reference length"),

            ScaleToolState.WaitingForReferenceSecondPoint => new CommandPromptState(
                "SCALE",
                "Specify second point of the reference length",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            ScaleToolState.WaitingForNewLength => new CommandPromptState(
                "SCALE",
                $"Specify new length{copySuffix} <{FormatNumber(_referenceLength ?? 1.0)}>",
                CommandInputKind.PointOrNumberOrOption,
                new[]
                {
                    new CommandOption("Points", "P", "Define the new length with two points")
                },
                acceptsEmptyEnter: true,
                placeholder: "new length, or click a point at that distance from the base point; P two points"),

            ScaleToolState.WaitingForNewLengthFirstPoint => new CommandPromptState(
                "SCALE",
                "Specify first point of the new length",
                CommandInputKind.Point,
                placeholder: "100,50"),

            ScaleToolState.WaitingForNewLengthSecondPoint => new CommandPromptState(
                "SCALE",
                "Specify second point of the new length",
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

        if (State == ScaleToolState.WaitingForEntitySelection)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return ConfirmEntitySelection(context);
            }

            return ToolResult.None("Select entities to scale, then press Enter/right-click.");
        }

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return State switch
            {
                ScaleToolState.WaitingForBasePoint => FinishCommand(context),
                ScaleToolState.WaitingForScaleFactor => ExecuteScale(context, s_lastScaleFactor),
                ScaleToolState.WaitingForReferenceLength => AcceptReferenceLength(context, s_lastReferenceLength),
                ScaleToolState.WaitingForNewLength => AcceptNewLength(context, _referenceLength ?? 1.0),
                _ => ToolResult.None("Specify the requested point.")
            };
        }

        if (input.Kind == CommandInputSubmissionKind.Option && input.OptionKeyword is not null)
        {
            return AcceptOption(context, input.OptionKeyword);
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            double value = input.Number.Value;

            return State switch
            {
                ScaleToolState.WaitingForScaleFactor => ExecuteScale(context, value),
                ScaleToolState.WaitingForReferenceLength => AcceptReferenceLength(context, value),
                ScaleToolState.WaitingForNewLength => AcceptNewLength(context, value),
                _ => ToolResult.None("Specify the requested point.")
            };
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "SCALE expects a point or scale factor input.");
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

        if (State == ScaleToolState.WaitingForEntitySelection)
        {
            return SelectEntityToScale(context, pointer);
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

        if (_basePoint is null || !IsDistanceState(State))
        {
            return ToolResult.None();
        }

        Point2D point = ResolvePointerPoint(context, pointer.ModelPoint);
        _currentDestinationPoint = point;

        double distance = _basePoint.Value.DistanceTo(point);

        if (State == ScaleToolState.WaitingForScaleFactor)
        {
            _currentFactor = distance;
        }
        else if (State == ScaleToolState.WaitingForNewLength)
        {
            _currentFactor = distance / (_referenceLength ?? 1.0);
        }
        else
        {
            return ToolResult.Updated("Point preview updated.");
        }

        return _currentFactor > 0
            ? ToolResult.Updated($"Scale: {FormatNumber(_currentFactor)}")
            : ToolResult.None("Scale factor must be greater than zero.");
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
            : context.EnabledSnaps & ~SnapKind.Entity;
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

        return EditableSelection.GetEntities(context)
            .Select(entity => entity.Transform(matrix))
            .ToList();
    }

    public ToolResult ConfirmEntitySelection(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (State != ScaleToolState.WaitingForEntitySelection)
        {
            return ToolResult.None();
        }

        int skipped = EditableSelection.Prune(context);
        if (!context.Selection.HasSelection)
        {
            return ToolResult.None(skipped > 0
                ? "The selected entities were deleted or are on hidden or locked layers. Select entities to scale."
                : "Select entities to scale.");
        }

        _skippedCount += skipped;
        State = ScaleToolState.WaitingForBasePoint;

        return ToolResult.Started("Specify base point for scaling.");
    }

    private static bool IsDistanceState(ScaleToolState state)
    {
        return state is
            ScaleToolState.WaitingForScaleFactor or
            ScaleToolState.WaitingForReferenceSecondPoint or
            ScaleToolState.WaitingForNewLength or
            ScaleToolState.WaitingForNewLengthSecondPoint;
    }

    private ToolResult AcceptPoint(
        ToolContext context,
        Point2D point)
    {
        return State switch
        {
            ScaleToolState.WaitingForBasePoint => AcceptBasePoint(context, point),
            ScaleToolState.WaitingForScaleFactor => AcceptScaleFactorPoint(context, point),
            ScaleToolState.WaitingForReferenceLength => AcceptPairFirstPoint(
                context,
                point,
                ScaleToolState.WaitingForReferenceSecondPoint,
                "Specify second point of the reference length."),
            ScaleToolState.WaitingForReferenceSecondPoint => AcceptReferenceLength(
                context,
                _pairFirstPoint!.Value.DistanceTo(point)),
            ScaleToolState.WaitingForNewLength => AcceptNewLength(
                context,
                _basePoint!.Value.DistanceTo(point)),
            ScaleToolState.WaitingForNewLengthFirstPoint => AcceptPairFirstPoint(
                context,
                point,
                ScaleToolState.WaitingForNewLengthSecondPoint,
                "Specify second point of the new length."),
            ScaleToolState.WaitingForNewLengthSecondPoint => AcceptNewLength(
                context,
                _pairFirstPoint!.Value.DistanceTo(point)),
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
            State = ScaleToolState.WaitingForEntitySelection;

            return ToolResult.Started(skipped > 0
                ? "The selected entities were deleted or are on hidden or locked layers. Select entities to scale."
                : "Select entities to scale.");
        }

        _skippedCount += skipped;
        _basePoint = point;
        _pairFirstPoint = null;
        _currentDestinationPoint = null;
        _referenceLength = null;
        _currentFactor = 1.0;
        State = ScaleToolState.WaitingForScaleFactor;
        context.CurrentBasePoint = point;

        return ToolResult.Started(
            $"Specify scale factor or [Copy/Reference] <{FormatNumber(s_lastScaleFactor)}>.");
    }

    private ToolResult AcceptOption(
        ToolContext context,
        string keyword)
    {
        if (State == ScaleToolState.WaitingForScaleFactor &&
            string.Equals(keyword, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            _isCopyMode = !_isCopyMode;

            return ToolResult.Started(_isCopyMode
                ? "Scaling a copy of the selected objects. Specify scale factor."
                : "Scaling the selected objects. Specify scale factor.");
        }

        if (State == ScaleToolState.WaitingForScaleFactor &&
            string.Equals(keyword, "Reference", StringComparison.OrdinalIgnoreCase))
        {
            _currentDestinationPoint = null;
            _currentFactor = 1.0;
            State = ScaleToolState.WaitingForReferenceLength;

            return ToolResult.Started(
                $"Specify reference length <{FormatNumber(s_lastReferenceLength)}>, or click the first point of the reference length.");
        }

        if (State == ScaleToolState.WaitingForNewLength &&
            string.Equals(keyword, "Points", StringComparison.OrdinalIgnoreCase))
        {
            _currentDestinationPoint = null;
            _currentFactor = 1.0;
            State = ScaleToolState.WaitingForNewLengthFirstPoint;

            return ToolResult.Started("Specify first point of the new length.");
        }

        return ToolResult.None($"Option {keyword} is not available now.");
    }

    private ToolResult AcceptScaleFactorPoint(
        ToolContext context,
        Point2D point)
    {
        double distance = _basePoint!.Value.DistanceTo(point);
        if (context.GeometryTolerance.IsDistanceZero(distance))
        {
            return ToolResult.None(
                "The point must be different from the base point.");
        }

        return ExecuteScale(context, distance);
    }

    private ToolResult AcceptPairFirstPoint(
        ToolContext context,
        Point2D point,
        ScaleToolState nextState,
        string message)
    {
        _pairFirstPoint = point;
        _currentDestinationPoint = null;
        context.CurrentBasePoint = point;
        State = nextState;

        return ToolResult.Started(message);
    }

    private ToolResult AcceptReferenceLength(
        ToolContext context,
        double length)
    {
        if (length <= 0 || context.GeometryTolerance.IsDistanceZero(length))
        {
            return ToolResult.None("Reference length must be greater than zero.");
        }

        _referenceLength = length;
        s_lastReferenceLength = length;
        _pairFirstPoint = null;
        _currentDestinationPoint = null;
        _currentFactor = 1.0;
        context.CurrentBasePoint = _basePoint;
        State = ScaleToolState.WaitingForNewLength;

        return ToolResult.Started(
            $"Reference length {FormatNumber(length)}. Specify new length or [Points].");
    }

    private ToolResult AcceptNewLength(
        ToolContext context,
        double length)
    {
        if (length <= 0 || context.GeometryTolerance.IsDistanceZero(length))
        {
            if (State == ScaleToolState.WaitingForNewLengthSecondPoint)
            {
                ReturnToNewLengthPrompt(context);
            }

            return ToolResult.None("New length must be greater than zero.");
        }

        ToolResult result = ExecuteScale(
            context,
            length / (_referenceLength ?? 1.0));

        if (result.Kind == ToolResultKind.None &&
            State == ScaleToolState.WaitingForNewLengthSecondPoint)
        {
            ReturnToNewLengthPrompt(context);
        }

        return result;
    }

    private void ReturnToNewLengthPrompt(ToolContext context)
    {
        _pairFirstPoint = null;
        _currentDestinationPoint = null;
        context.CurrentBasePoint = _basePoint;
        State = ScaleToolState.WaitingForNewLength;
    }

    private ToolResult ExecuteScale(
        ToolContext context,
        double factor)
    {
        if (_basePoint is null)
        {
            return ToolResult.None("Specify the base point first.");
        }

        if (factor <= 0 || Tolerance.IsZero(factor) || double.IsNaN(factor) || double.IsInfinity(factor))
        {
            return ToolResult.None("Scale factor must be greater than zero.");
        }

        // A factor of 1 leaves the geometry unchanged; executing it would only add an
        // empty undo step and mark the document as modified.
        if (Tolerance.IsZero(factor - 1.0))
        {
            return ToolResult.None(
                "Scale factor is 1, so the selection would not change. Specify a different factor or point.");
        }

        int skipped = _skippedCount + EditableSelection.Prune(context);
        IReadOnlyList<CadEntity> entities = EditableSelection.GetEntities(context);

        if (entities.Count == 0)
        {
            Reset(context);

            return ToolResult.None("No editable entities selected." + EditableSelection.FormatSkippedSuffix(skipped));
        }

        bool isCopy = _isCopyMode;

        if (isCopy)
        {
            Matrix2D matrix = Matrix2D.Scale(factor, _basePoint.Value);
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
                new ScaleEntitiesCommand(
                    entities.Select(entity => entity.Id).ToList(),
                    _basePoint.Value,
                    factor));
        }

        s_lastScaleFactor = factor;

        Reset(context);

        return ToolResult.Completed(
            (isCopy ? "Scaled copy created" : "Entities scaled") +
            $" by {FormatNumber(factor)}." +
            EditableSelection.FormatSkippedSuffix(skipped));
    }

    private Point2D ResolvePointerPoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        Point2D? origin = GuideOrigin;
        Point2D point = ApplyGeometricSnap(context, cursorPoint, origin);

        return origin is not null && IsDistanceState(State)
            ? ToolInputConstraintService.ApplyAngleConstraint(context, origin.Value, point)
            : point;
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
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

        State = EditableSelection.HasAny(context)
            ? ScaleToolState.WaitingForBasePoint
            : ScaleToolState.WaitingForEntitySelection;
    }

    private ToolResult FinishCommand(ToolContext context)
    {
        Reset(context);
        IsCommandFinished = true;

        return ToolResult.Completed("Scale command finished.");
    }

    private void Reset(ToolContext? context = null)
    {
        _basePoint = null;
        _pairFirstPoint = null;
        _currentDestinationPoint = null;
        _referenceLength = null;
        _isCopyMode = false;
        _currentFactor = 1.0;
        _skippedCount = 0;
        IsCommandFinished = false;
        State = context is not null && !EditableSelection.HasAny(context)
            ? ScaleToolState.WaitingForEntitySelection
            : ScaleToolState.WaitingForBasePoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
