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
/// Mirrors the current selection across a two-point axis.
/// By default the source entities are kept, matching the common CAD workflow.
/// </summary>
public sealed class MirrorTool : ICadTool, ISnapModeProvider, ICommandDrivenTool, IToolPreviewDescriptorProvider
{
    private static readonly CommandOption YesOption = new("Yes", "Y", "Delete source entities after mirroring");
    private static readonly CommandOption NoOption = new("No", "N", "Keep source entities after mirroring");

    private Point2D? _firstAxisPoint;
    private Point2D? _secondAxisPoint;
    private MirrorToolState _state;
    private int _skippedCount;

    public MirrorTool()
    {
        _state = MirrorToolState.WaitingForFirstAxisPoint;
    }

    public string Name => "Mirror";

    public MirrorToolState State => _state;

    public bool IsCommandFinished { get; private set; }

    public Point2D? FirstAxisPoint => _firstAxisPoint;

    public Point2D? SecondAxisPoint => _secondAxisPoint;

    public bool HasPreview =>
        _firstAxisPoint.HasValue &&
        _secondAxisPoint.HasValue &&
        (_state == MirrorToolState.WaitingForSecondAxisPoint ||
         _state == MirrorToolState.WaitingForDeleteSourceOption);

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        return _state switch
        {
            MirrorToolState.WaitingForEntitySelection => new CommandPromptState(
                "MIRROR",
                "Select objects to mirror",
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Click objects, then press Enter/right-click"),

            MirrorToolState.WaitingForFirstAxisPoint => new CommandPromptState(
                "MIRROR",
                "Specify first point of mirror line or press Enter to finish",
                CommandInputKind.Point,
                acceptsEmptyEnter: true,
                placeholder: "100,50   |   Enter finishes"),

            MirrorToolState.WaitingForSecondAxisPoint => new CommandPromptState(
                "MIRROR",
                "Specify second point of mirror line",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            MirrorToolState.WaitingForDeleteSourceOption => new CommandPromptState(
                "MIRROR",
                "Delete source objects? <No>",
                CommandInputKind.Option,
                new[] { YesOption, NoOption },
                acceptsEmptyEnter: true,
                placeholder: "Yes/No"),

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

        if (_state == MirrorToolState.WaitingForEntitySelection)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return ConfirmEntitySelection(context);
            }

            return ToolResult.None("Select entities to mirror, then press Enter/right-click.");
        }

        if (_state == MirrorToolState.WaitingForDeleteSourceOption)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return ExecuteMirror(context, deleteSource: false);
            }

            if (input.Kind == CommandInputSubmissionKind.Option)
            {
                return HandleDeleteSourceOption(context, input.OptionKeyword);
            }

            return ToolResult.None(input.ErrorMessage ?? "Type Yes or No.");
        }

        if (_state == MirrorToolState.WaitingForFirstAxisPoint &&
            input.Kind == CommandInputSubmissionKind.Confirm)
        {
            Reset(context);
            IsCommandFinished = true;

            return ToolResult.Completed("Mirror command finished.");
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "MIRROR expects a point input.");
        }

        return _state switch
        {
            MirrorToolState.WaitingForFirstAxisPoint => AcceptFirstAxisPoint(context, input.Point.Value),
            MirrorToolState.WaitingForSecondAxisPoint => AcceptSecondAxisPoint(context, input.Point.Value),
            _ => ToolResult.None()
        };
    }

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        // The tool snaps axis points without entity snap, so the canvas must not offer it either.
        return _state == MirrorToolState.WaitingForEntitySelection
            ? SnapKind.EntityOnly
            : context.EnabledSnaps & ~SnapKind.Entity;
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!HasPreview || _firstAxisPoint is null || _secondAxisPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        Matrix2D matrix = Matrix2D.Mirror(Line2D.FromPoints(
            _firstAxisPoint.Value,
            _secondAxisPoint.Value));

        return EditableSelection.GetEntities(context)
            .Select(entity => entity.Transform(matrix).WithId(EntityId.New()))
            .ToList();
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<CadEntity> entities = GetPreviewEntities(context);

        if (_firstAxisPoint is null)
        {
            return ToolPreviewDescriptor.FromEntities(entities);
        }

        var markers = new List<ToolPreviewMarker>
        {
            new(_firstAxisPoint.Value, ToolPreviewMarkerKind.Primary)
        };

        var lines = new List<ToolPreviewLine>();

        if (_secondAxisPoint is not null)
        {
            lines.Add(new ToolPreviewLine(
                _firstAxisPoint.Value,
                _secondAxisPoint.Value,
                ToolPreviewLineKind.Axis));

            markers.Add(new ToolPreviewMarker(
                _secondAxisPoint.Value,
                ToolPreviewMarkerKind.Primary));
        }

        return new ToolPreviewDescriptor(
            entities: entities,
            lines: lines,
            markers: markers);
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
            MirrorToolState.WaitingForEntitySelection => SelectEntityToMirror(context, pointer),
            MirrorToolState.WaitingForFirstAxisPoint => AcceptFirstAxisPoint(
                context,
                ApplyGeometricSnap(context, pointer.ModelPoint, null)),
            MirrorToolState.WaitingForSecondAxisPoint => AcceptSecondAxisPoint(
                context,
                ApplyPointerConstraints(context, pointer.ModelPoint)),
            MirrorToolState.WaitingForDeleteSourceOption => ToolResult.None("Type Yes or No, or press Enter/right-click to keep source objects."),
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

        if (_state != MirrorToolState.WaitingForSecondAxisPoint || _firstAxisPoint is null)
        {
            return ToolResult.None();
        }

        Point2D point = ApplyPointerConstraints(context, pointer.ModelPoint);

        if (context.GeometryTolerance.ArePointsEqual(_firstAxisPoint.Value, point))
        {
            _secondAxisPoint = null;
            return ToolResult.None("Second mirror point must be different from first point.");
        }

        _secondAxisPoint = point;
        return ToolResult.Updated("Mirror preview updated.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Mirror command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Mirror tool deactivated.");
    }

    public ToolResult ConfirmEntitySelection(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureInitialState(context);

        if (_state != MirrorToolState.WaitingForEntitySelection)
        {
            return ToolResult.None();
        }

        int skipped = EditableSelection.Prune(context);
        if (!context.Selection.HasSelection)
        {
            return ToolResult.None(skipped > 0
                ? "The selected entities were deleted or are on hidden or locked layers. Select entities to mirror."
                : "Select entities to mirror.");
        }

        _skippedCount += skipped;
        _state = MirrorToolState.WaitingForFirstAxisPoint;

        return ToolResult.Started("Specify first point of mirror line.");
    }

    private ToolResult SelectEntityToMirror(
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
            return ToolResult.None("Select entities to mirror, then press Enter/right-click.");
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
            ? "Overlapping entity selected. Press Enter/right-click to specify mirror line."
            : "Entity selected. Press Enter/right-click to specify mirror line.");
    }

    private ToolResult AcceptFirstAxisPoint(
        ToolContext context,
        Point2D point)
    {
        int skipped = EditableSelection.Prune(context);
        if (!context.Selection.HasSelection)
        {
            _state = MirrorToolState.WaitingForEntitySelection;

            return ToolResult.Started(skipped > 0
                ? "The selected entities were deleted or are on hidden or locked layers. Select entities to mirror."
                : "Select entities to mirror.");
        }

        _skippedCount += skipped;
        _firstAxisPoint = point;
        _secondAxisPoint = null;
        context.CurrentBasePoint = point;
        _state = MirrorToolState.WaitingForSecondAxisPoint;

        return ToolResult.Started("Specify second point of mirror line.");
    }

    private ToolResult AcceptSecondAxisPoint(
        ToolContext context,
        Point2D point)
    {
        if (_firstAxisPoint is null)
        {
            throw new InvalidOperationException("Cannot accept second mirror point before first point.");
        }

        // Pointer input is snapped and constrained before reaching this method; typed
        // coordinates are explicit and must stay exact, so no ortho/polar adjustment here.
        if (context.GeometryTolerance.ArePointsEqual(_firstAxisPoint.Value, point))
        {
            return ToolResult.None("Second mirror point must be different from first point.");
        }

        _secondAxisPoint = point;

        _state = MirrorToolState.WaitingForDeleteSourceOption;
        context.CurrentBasePoint = null;

        return ToolResult.Started("Delete source objects? [Yes/No] <No>.");
    }

    private ToolResult HandleDeleteSourceOption(
        ToolContext context,
        string? optionKeyword)
    {
        if (string.Equals(optionKeyword, "Yes", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteMirror(context, deleteSource: true);
        }

        if (string.Equals(optionKeyword, "No", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteMirror(context, deleteSource: false);
        }

        return ToolResult.None("Type Yes or No.");
    }

    private ToolResult ExecuteMirror(
        ToolContext context,
        bool deleteSource)
    {
        if (_firstAxisPoint is null || _secondAxisPoint is null)
        {
            return ToolResult.None("Specify mirror line before completing the command.");
        }

        int skipped = _skippedCount + EditableSelection.Prune(context);
        IReadOnlyList<CadEntity> entities = EditableSelection.GetEntities(context);

        if (entities.Count == 0)
        {
            Reset(context);
            return ToolResult.None("No editable entities selected." + EditableSelection.FormatSkippedSuffix(skipped));
        }

        Line2D mirrorLine = Line2D.FromPoints(_firstAxisPoint.Value, _secondAxisPoint.Value);

        if (deleteSource)
        {
            context.Commands.Execute(
                context.Document,
                new MirrorEntitiesCommand(
                    entities.Select(entity => entity.Id).ToList(),
                    mirrorLine));

            Reset(context);

            return ToolResult.Completed("Entities mirrored." + EditableSelection.FormatSkippedSuffix(skipped));
        }

        Matrix2D matrix = Matrix2D.Mirror(mirrorLine);
        IReadOnlyList<CadEntity> mirroredEntities = entities
            .Select(entity => entity.Transform(matrix).WithId(EntityId.New()))
            .ToList();

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(mirroredEntities));

        Reset(context);

        return ToolResult.Completed("Mirrored copy created." + EditableSelection.FormatSkippedSuffix(skipped));
    }

    private Point2D ApplyPointerConstraints(
        ToolContext context,
        Point2D cursorPoint)
    {
        Point2D point = ApplyGeometricSnap(context, cursorPoint, _firstAxisPoint);

        return _firstAxisPoint is null
            ? point
            : ToolInputConstraintService.ApplyAngleConstraint(context, _firstAxisPoint.Value, point);
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
        if (_firstAxisPoint is not null ||
            _state == MirrorToolState.WaitingForDeleteSourceOption)
        {
            return;
        }

        if (_state == MirrorToolState.WaitingForEntitySelection &&
            context.Selection.HasSelection)
        {
            return;
        }

        _state = EditableSelection.HasAny(context)
            ? MirrorToolState.WaitingForFirstAxisPoint
            : MirrorToolState.WaitingForEntitySelection;
    }

    private void Reset(ToolContext context)
    {
        _firstAxisPoint = null;
        _secondAxisPoint = null;
        _skippedCount = 0;
        IsCommandFinished = false;
        context.CurrentBasePoint = null;

        _state = EditableSelection.HasAny(context)
            ? MirrorToolState.WaitingForFirstAxisPoint
            : MirrorToolState.WaitingForEntitySelection;
    }
}
