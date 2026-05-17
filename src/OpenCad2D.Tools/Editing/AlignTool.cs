using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Transformations;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Interactive tool used to align the current selection by mapping two source
/// points to two destination points.
/// </summary>
public sealed class AlignTool : ICadTool, ICommandDrivenTool, IKeyboardAwareTool, IToolPreviewEntityProvider
{
    private readonly AlignTransformService _alignTransformService;
    private Point2D? _sourcePoint1;
    private Point2D? _destinationPoint1;
    private Point2D? _sourcePoint2;
    private Point2D? _destinationPoint2;
    private AlignTransformResult? _currentTransform;

    public AlignTool()
        : this(new AlignTransformService())
    {
    }

    public AlignTool(AlignTransformService alignTransformService)
    {
        _alignTransformService = alignTransformService;
    }

    public string Name => "Align";

    public AlignToolState State { get; private set; } =
        AlignToolState.WaitingForSourcePoint1;

    public Point2D? SourcePoint1 => _sourcePoint1;

    public Point2D? DestinationPoint1 => _destinationPoint1;

    public Point2D? SourcePoint2 => _sourcePoint2;

    public Point2D? DestinationPoint2 => _destinationPoint2;

    public AlignTransformResult? CurrentTransform => _currentTransform;

    public bool HasPreview =>
        (State == AlignToolState.WaitingForDestinationPoint2 ||
         State == AlignToolState.WaitingForScaleConfirmation) &&
        _currentTransform is not null;


    private static readonly CommandOption YesOption = new("Yes", "Y", "Apply scale while aligning");
    private static readonly CommandOption NoOption = new("No", "N", "Align without scale");

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Selection.HasSelection)
        {
            return new CommandPromptState(
                "ALIGN",
                "Select objects before aligning",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Select objects, then run ALIGN again");
        }

        return State switch
        {
            AlignToolState.WaitingForSourcePoint1 => new CommandPromptState(
                "ALIGN",
                "Specify first source point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            AlignToolState.WaitingForDestinationPoint1 => new CommandPromptState(
                "ALIGN",
                "Specify first destination point",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            AlignToolState.WaitingForSourcePoint2 => new CommandPromptState(
                "ALIGN",
                "Specify second source point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            AlignToolState.WaitingForDestinationPoint2 => new CommandPromptState(
                "ALIGN",
                "Specify second destination point",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            AlignToolState.WaitingForScaleConfirmation => new CommandPromptState(
                "ALIGN",
                "Apply scale",
                CommandInputKind.Option,
                new[] { YesOption, NoOption },
                acceptsEmptyEnter: true,
                placeholder: "Y or N"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Selection.HasSelection)
        {
            return ToolResult.None("Select entities before running Align.");
        }

        if (State == AlignToolState.WaitingForScaleConfirmation)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return ConfirmWithoutScale(context);
            }

            if (input.Kind == CommandInputSubmissionKind.Option)
            {
                return string.Equals(input.OptionKeyword, "Yes", StringComparison.OrdinalIgnoreCase)
                    ? ConfirmWithScale(context)
                    : ConfirmWithoutScale(context);
            }

            return ToolResult.None(input.ErrorMessage ?? "ALIGN expects Y, N or Enter.");
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "ALIGN expects a point input.");
        }

        return State switch
        {
            AlignToolState.WaitingForSourcePoint1 => AcceptSourcePoint1(context, input.Point.Value),
            AlignToolState.WaitingForDestinationPoint1 => AcceptDestinationPoint1(context, input.Point.Value),
            AlignToolState.WaitingForSourcePoint2 => AcceptSourcePoint2(context, input.Point.Value),
            AlignToolState.WaitingForDestinationPoint2 => AcceptDestinationPoint2(context, input.Point.Value),
            _ => ToolResult.None()
        };
    }

    public bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State == AlignToolState.WaitingForScaleConfirmation &&
            key == CadToolKey.Enter)
        {
            result = ConfirmWithoutScale(context);
            return true;
        }

        if (State == AlignToolState.WaitingForScaleConfirmation &&
            key == CadToolKey.S)
        {
            result = ConfirmWithScale(context);
            return true;
        }

        result = ToolResult.None();
        return false;
    }

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

        if (State == AlignToolState.WaitingForScaleConfirmation)
        {
            return ToolResult.None(
                "Press Enter or N to align without scale, or Y to align with scale.");
        }

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            context.CurrentBasePoint);

        return State switch
        {
            AlignToolState.WaitingForSourcePoint1 =>
                AcceptSourcePoint1(context, point),

            AlignToolState.WaitingForDestinationPoint1 =>
                AcceptDestinationPoint1(context, point),

            AlignToolState.WaitingForSourcePoint2 =>
                AcceptSourcePoint2(context, point),

            AlignToolState.WaitingForDestinationPoint2 =>
                AcceptDestinationPoint2(context, point),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != AlignToolState.WaitingForDestinationPoint2 ||
            _sourcePoint1 is null ||
            _destinationPoint1 is null ||
            _sourcePoint2 is null)
        {
            return ToolResult.None();
        }

        Point2D point = ApplySnap(
            context,
            pointer.ModelPoint,
            _destinationPoint1);

        UpdatePreview(
            point,
            applyScale: false);

        return ToolResult.Updated(
            $"Align angle: {_currentTransform?.RotationDegrees:0.##}°");
    }

    public ToolResult ConfirmWithoutScale(ToolContext context)
    {
        return Confirm(
            context,
            applyScale: false);
    }

    public ToolResult ConfirmWithScale(ToolContext context)
    {
        return Confirm(
            context,
            applyScale: true);
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Align command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Align tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!HasPreview || _currentTransform is null)
        {
            return Array.Empty<CadEntity>();
        }

        return context.Document.Entities
            .GetByIds(context.Selection.SelectedIds)
            .Select(entity => entity.Transform(_currentTransform.Matrix))
            .ToList();
    }

    private ToolResult AcceptSourcePoint1(
        ToolContext context,
        Point2D point)
    {
        _sourcePoint1 = point;
        _destinationPoint1 = null;
        _sourcePoint2 = null;
        _destinationPoint2 = null;
        _currentTransform = null;
        State = AlignToolState.WaitingForDestinationPoint1;
        context.CurrentBasePoint = point;

        return ToolResult.Started("Specify first destination point for align.");
    }

    private ToolResult AcceptDestinationPoint1(
        ToolContext context,
        Point2D point)
    {
        if (_sourcePoint1 is null)
        {
            throw new InvalidOperationException(
                "Cannot accept first destination point before first source point.");
        }

        _destinationPoint1 = point;
        _sourcePoint2 = null;
        _destinationPoint2 = null;
        _currentTransform = null;
        State = AlignToolState.WaitingForSourcePoint2;
        context.CurrentBasePoint = point;

        return ToolResult.Started("Specify second source point for align.");
    }

    private ToolResult AcceptSourcePoint2(
        ToolContext context,
        Point2D point)
    {
        if (_sourcePoint1 is null || _destinationPoint1 is null)
        {
            throw new InvalidOperationException(
                "Cannot accept second source point before first source and destination points.");
        }

        if (AreSamePoint(
                _sourcePoint1.Value,
                point,
                context))
        {
            return ToolResult.None(
                "Second source point must be different from first source point.");
        }

        _sourcePoint2 = point;
        _destinationPoint2 = null;
        _currentTransform = null;
        State = AlignToolState.WaitingForDestinationPoint2;
        context.CurrentBasePoint = _destinationPoint1.Value;

        return ToolResult.Started("Specify second destination point for align.");
    }

    private ToolResult AcceptDestinationPoint2(
        ToolContext context,
        Point2D point)
    {
        if (_sourcePoint1 is null ||
            _destinationPoint1 is null ||
            _sourcePoint2 is null)
        {
            throw new InvalidOperationException(
                "Cannot accept second destination point before all previous align points.");
        }

        if (AreSamePoint(
                _destinationPoint1.Value,
                point,
                context))
        {
            return ToolResult.None(
                "Second destination point must be different from first destination point.");
        }

        UpdatePreview(
            point,
            applyScale: false);

        if (_currentTransform is null)
        {
            return ToolResult.None("Cannot calculate align transformation.");
        }

        State = AlignToolState.WaitingForScaleConfirmation;
        context.CurrentBasePoint = null;

        return ToolResult.Started(
            "Apply scale? Press Enter/N for No, or Y for Yes.");
    }

    private ToolResult Confirm(
        ToolContext context,
        bool applyScale)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State != AlignToolState.WaitingForScaleConfirmation)
        {
            return ToolResult.None("Align is not waiting for scale confirmation.");
        }

        if (_sourcePoint1 is null ||
            _destinationPoint1 is null ||
            _sourcePoint2 is null ||
            _destinationPoint2 is null)
        {
            return ToolResult.None("Cannot calculate align transformation.");
        }

        UpdatePreview(
            _destinationPoint2.Value,
            applyScale);

        if (_currentTransform is null)
        {
            return ToolResult.None("Cannot calculate align transformation.");
        }

        IReadOnlyList<EntityId> selectedIds = context.Selection.SelectedIds.ToList();

        context.Commands.Execute(
            context.Document,
            new TransformEntitiesCommand(
                selectedIds,
                _currentTransform.Matrix));

        double angleDegrees = _currentTransform.RotationDegrees;
        double scaleFactor = _currentTransform.ScaleFactor;

        Reset(context);

        return applyScale
            ? ToolResult.Completed(
                $"Entities aligned. Angle: {angleDegrees:0.##}°. Scale: {scaleFactor:0.###}.")
            : ToolResult.Completed(
                $"Entities aligned. Angle: {angleDegrees:0.##}°.");
    }

    private void UpdatePreview(
        Point2D destinationPoint2,
        bool applyScale)
    {
        if (_sourcePoint1 is null ||
            _destinationPoint1 is null ||
            _sourcePoint2 is null)
        {
            _currentTransform = null;
            return;
        }

        _destinationPoint2 = destinationPoint2;
        _currentTransform = _alignTransformService.Calculate(
            _sourcePoint1.Value,
            _destinationPoint1.Value,
            _sourcePoint2.Value,
            destinationPoint2,
            applyScale);
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
        _sourcePoint1 = null;
        _destinationPoint1 = null;
        _sourcePoint2 = null;
        _destinationPoint2 = null;
        _currentTransform = null;
        State = AlignToolState.WaitingForSourcePoint1;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
