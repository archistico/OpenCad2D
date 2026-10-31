using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Transformations;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Editing;

/// <summary>Aligns a confirmed selection using two source/destination point pairs.</summary>
public sealed class AlignTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider,
    IToolPreviewDescriptorProvider, ISnapModeProvider
{
    private readonly AlignTransformService _alignTransformService;
    private readonly SelectionTool _selectionTool = new();
    private readonly List<CadEntity> _objects = new();
    private bool _initialized;
    private Point2D? _sourcePoint1;
    private Point2D? _destinationPoint1;
    private Point2D? _sourcePoint2;
    private Point2D? _destinationPoint2;
    private Point2D? _hoverDestination1;
    private Matrix2D? _translationPreview;
    private AlignTransformResult? _currentTransform;
    private string? _feedback;

    public AlignTool() : this(new AlignTransformService()) { }

    public AlignTool(AlignTransformService alignTransformService)
    {
        ArgumentNullException.ThrowIfNull(alignTransformService);
        _alignTransformService = alignTransformService;
    }

    public string Name => "Align";
    public AlignToolState State { get; private set; } = AlignToolState.WaitingForSourcePoint1;
    public bool IsCommandFinished { get; private set; }
    public Point2D? SourcePoint1 => _sourcePoint1;
    public Point2D? DestinationPoint1 => _destinationPoint1;
    public Point2D? SourcePoint2 => _sourcePoint2;
    public Point2D? DestinationPoint2 => _destinationPoint2;
    public AlignTransformResult? CurrentTransform => _currentTransform;
    public bool HasPreview => _currentTransform is not null || _translationPreview is not null;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        EnsureState(context);
        var options = new List<CommandOption>();
        if (State != AlignToolState.WaitingForEntitySelection)
        {
            options.Add(new CommandOption("Objects", "O", "Choose objects again without changing the drawing"));
            if (State != AlignToolState.WaitingForSourcePoint1)
                options.Add(new CommandOption("Back", "B", "Correct the previous reference point"));
        }
        if (State == AlignToolState.WaitingForScaleConfirmation)
        {
            options.Add(new CommandOption("Yes", "Y", "Align with uniform scale so both point pairs coincide"));
            options.Add(new CommandOption("No", "N", "Preserve object size; match the first point and direction"));
        }
        string prompt = State switch
        {
            AlignToolState.WaitingForEntitySelection => $"Select objects ({context.Selection.SelectedIds.Count}). Click/Shift/Ctrl or drag a window; Enter/right-click continues.",
            AlignToolState.WaitingForSourcePoint1 => "1/5: Pick the FIRST SOURCE point on the objects to align.",
            AlignToolState.WaitingForDestinationPoint1 => "2/5: Pick where the FIRST SOURCE point should go (first destination).",
            AlignToolState.WaitingForSourcePoint2 => "3/5: Pick the SECOND SOURCE point to define the original direction.",
            AlignToolState.WaitingForDestinationPoint2 => "4/5: Pick the SECOND DESTINATION point to define the new direction.",
            _ => "5/5: Apply scale? Y matches both pairs; N or Enter/right-click preserves size [No]."
        };
        CommandInputKind kind = State == AlignToolState.WaitingForEntitySelection
            ? CommandInputKind.Selection
            : State == AlignToolState.WaitingForScaleConfirmation ? CommandInputKind.Option : CommandInputKind.PointOrOption;
        return new CommandPromptState("ALIGN", prompt, kind, options,
            acceptsEmptyEnter: State is AlignToolState.WaitingForEntitySelection or AlignToolState.WaitingForScaleConfirmation,
            placeholder: kind == CommandInputKind.Selection ? "Select objects, then Enter/right-click" : "Pick on canvas or enter X,Y; B corrects the previous point",
            feedback: _feedback);
    }

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        EnsureState(context);
        return State == AlignToolState.WaitingForEntitySelection ? SnapKind.EntityOnly
            : State == AlignToolState.WaitingForScaleConfirmation ? SnapKind.None
            : context.EnabledSnaps & ~SnapKind.Entity;
    }

    public ToolResult HandleCommandInput(CommandInputSubmission input, ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!EnsureState(context))
            return ToolResult.None(_feedback);
        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            if (string.Equals(input.OptionKeyword, "Objects", StringComparison.OrdinalIgnoreCase))
            {
                BeginObjectSelection(context);
                return Report(ToolResult.Started("Choose objects again, then Enter/right-click. The drawing is unchanged."));
            }
            if (string.Equals(input.OptionKeyword, "Back", StringComparison.OrdinalIgnoreCase))
                return Back(context);
            if (State == AlignToolState.WaitingForScaleConfirmation)
            {
                if (string.Equals(input.OptionKeyword, "Yes", StringComparison.OrdinalIgnoreCase))
                    return Confirm(context, true);
                if (string.Equals(input.OptionKeyword, "No", StringComparison.OrdinalIgnoreCase))
                    return Confirm(context, false);
            }
        }
        if (State == AlignToolState.WaitingForEntitySelection)
        {
            if (input.Kind != CommandInputSubmissionKind.Confirm || !CaptureObjects(context))
                return Report(ToolResult.None("Select editable objects on the canvas, then Enter/right-click to choose reference points."));
            _selectionTool.Deactivate(context);
            State = AlignToolState.WaitingForSourcePoint1;
            return Report(ToolResult.Started("Objects selected. Pick the FIRST SOURCE point."));
        }
        if (State == AlignToolState.WaitingForScaleConfirmation)
            return input.Kind == CommandInputSubmissionKind.Confirm ? Confirm(context, false)
                : Report(ToolResult.None("Choose Y/Yes, N/No or Enter/right-click for No. B corrects the last destination."));
        return input.Kind == CommandInputSubmissionKind.Point && input.Point is { } point
            ? AcceptPoint(context, point)
            : Report(ToolResult.None(input.ErrorMessage ?? "Pick the requested reference point or enter coordinates; B goes back."));
    }

    public ToolResult OnPointerPressed(ToolContext context, PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        if (!EnsureState(context))
            return ToolResult.None(_feedback);
        if (State == AlignToolState.WaitingForEntitySelection)
            return _selectionTool.OnPointerPressed(context, pointer);
        if (State == AlignToolState.WaitingForScaleConfirmation)
            return Report(ToolResult.None("Choose Y/Yes, N/No or Enter/right-click for No. B corrects the last destination."));
        return AcceptPoint(context, ApplySnap(context, pointer.ModelPoint));
    }

    public ToolResult OnPointerReleased(ToolContext context, PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        if (!EnsureState(context) || State != AlignToolState.WaitingForEntitySelection)
            return ToolResult.None();
        ToolResult result = _selectionTool.OnPointerReleased(context, pointer);
        return Report(ToolResult.Updated($"{context.Selection.SelectedIds.Count} objects selected. Shift toggles; Ctrl cycles overlaps; Enter/right-click continues. {result.Message}"));
    }

    public ToolResult OnPointerMoved(ToolContext context, PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        if (!EnsureState(context))
            return ToolResult.None(_feedback);
        if (State == AlignToolState.WaitingForEntitySelection)
            return _selectionTool.OnPointerMoved(context, pointer);
        Point2D point = ApplySnap(context, pointer.ModelPoint);
        if (State == AlignToolState.WaitingForDestinationPoint1 && _sourcePoint1 is { } source)
        {
            _hoverDestination1 = point;
            _translationPreview = Matrix2D.Translation(point.X - source.X, point.Y - source.Y);
            return Report(ToolResult.Updated("Pick the first destination. Green previews the moved objects; no change is committed yet."));
        }
        if (State == AlignToolState.WaitingForDestinationPoint2)
        {
            _translationPreview = null;
            bool valid = UpdateTransform(context, point, false);
            return Report(valid ? ToolResult.Updated($"Align preview without scale: {_currentTransform!.RotationDegrees:0.##} degrees. Pick the second destination, then choose Yes/No.")
                : ToolResult.None("The second destination must be distinct from the first. Move away from the first destination."));
        }
        return ToolResult.None();
    }

    public ToolResult ConfirmWithoutScale(ToolContext context) => Confirm(context, false);
    public ToolResult ConfirmWithScale(ToolContext context) => Confirm(context, true);

    private ToolResult AcceptPoint(ToolContext context, Point2D point)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
            return Report(ToolResult.None("Reference point coordinates must be finite."));
        switch (State)
        {
            case AlignToolState.WaitingForSourcePoint1:
                _sourcePoint1 = point;
                State = AlignToolState.WaitingForDestinationPoint1;
                context.CurrentBasePoint = point;
                break;
            case AlignToolState.WaitingForDestinationPoint1:
                _destinationPoint1 = point;
                _hoverDestination1 = null;
                _translationPreview = Matrix2D.Translation(point.X - _sourcePoint1!.Value.X, point.Y - _sourcePoint1.Value.Y);
                State = AlignToolState.WaitingForSourcePoint2;
                context.CurrentBasePoint = _sourcePoint1;
                break;
            case AlignToolState.WaitingForSourcePoint2:
                if (context.GeometryTolerance.ArePointsEqual(_sourcePoint1!.Value, point))
                    return Report(ToolResult.None("Second source point must be different from first source point."));
                _sourcePoint2 = point;
                _translationPreview = null;
                State = AlignToolState.WaitingForDestinationPoint2;
                context.CurrentBasePoint = _destinationPoint1;
                break;
            case AlignToolState.WaitingForDestinationPoint2:
                _translationPreview = null;
                if (!UpdateTransform(context, point, false))
                    return Report(ToolResult.None("Second destination point must be different from first destination point and define a valid transformation."));
                State = AlignToolState.WaitingForScaleConfirmation;
                context.CurrentBasePoint = null;
                return Report(ToolResult.Started("Apply scale? Y matches both point pairs; N or Enter/right-click keeps the original size. B corrects the last point."));
            default:
                return ToolResult.None();
        }
        return Report(ToolResult.Started(GetPromptState(context).Prompt));
    }

    private bool UpdateTransform(ToolContext context, Point2D destination, bool scale)
    {
        _currentTransform = null;
        _destinationPoint2 = null;
        if (_sourcePoint1 is not { } source1 || _sourcePoint2 is not { } source2 ||
            _destinationPoint1 is not { } destination1 ||
            !double.IsFinite(destination.X) || !double.IsFinite(destination.Y) ||
            context.GeometryTolerance.ArePointsEqual(destination1, destination))
            return false;
        AlignTransformResult result = _alignTransformService.Calculate(source1, destination1, source2, destination, scale);
        Matrix2D matrix = result.Matrix;
        if (result.IsDegenerate || !double.IsFinite(result.ScaleFactor) || result.ScaleFactor <= 0 ||
            !double.IsFinite(matrix.M11) || !double.IsFinite(matrix.M12) || !double.IsFinite(matrix.M21) ||
            !double.IsFinite(matrix.M22) || !double.IsFinite(matrix.OffsetX) || !double.IsFinite(matrix.OffsetY))
            return false;
        _destinationPoint2 = destination;
        _currentTransform = result;
        return true;
    }

    private ToolResult Confirm(ToolContext context, bool applyScale)
    {
        if (!EnsureState(context))
            return ToolResult.None(_feedback);
        if (State != AlignToolState.WaitingForScaleConfirmation || _destinationPoint2 is not { } destination)
            return Report(ToolResult.None("Align is not waiting for scale confirmation."));
        if (!UpdateTransform(context, destination, applyScale))
            return Report(ToolResult.None("Cannot calculate a finite align transformation. B corrects the last destination; O chooses objects again."));
        context.Commands.Execute(context.Document,
            new TransformEntitiesCommand(_objects.Select(entity => entity.Id), _currentTransform!.Matrix));
        double angle = _currentTransform.RotationDegrees;
        double scale = _currentTransform.ScaleFactor;
        Reset(context);
        IsCommandFinished = true;
        return Report(ToolResult.Completed($"Objects aligned. Angle: {angle:0.##} degrees. Scale: {scale:0.###}."));
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        EnsureState(context);
        Matrix2D? matrix = _currentTransform?.Matrix ?? _translationPreview;
        return matrix is { } transform ? _objects.Select(entity => entity.Transform(transform)).ToArray() : Array.Empty<CadEntity>();
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        EnsureState(context);
        if (State == AlignToolState.WaitingForEntitySelection)
            return _selectionTool.GetPreviewDescriptor(context);
        var markers = new List<ToolPreviewMarker>();
        var lines = new List<ToolPreviewLine>();
        AddPair(_sourcePoint1, _destinationPoint1 ?? _hoverDestination1);
        AddPair(_sourcePoint2, _destinationPoint2);
        return new ToolPreviewDescriptor(entities: GetPreviewEntities(context), lines: lines, markers: markers,
            entityHighlightKind: ToolPreviewHighlightKind.Addition);

        void AddPair(Point2D? source, Point2D? destination)
        {
            if (source is { } s)
                markers.Add(new ToolPreviewMarker(s, ToolPreviewMarkerKind.Primary, ToolPreviewMarkerShape.Square));
            if (destination is { } d)
                markers.Add(new ToolPreviewMarker(d, ToolPreviewMarkerKind.Secondary, ToolPreviewMarkerShape.Circle));
            if (source is { } start && destination is { } end)
                lines.Add(new ToolPreviewLine(start, end));
        }
    }

    private ToolResult Back(ToolContext context)
    {
        _currentTransform = null;
        _translationPreview = null;
        _hoverDestination1 = null;
        switch (State)
        {
            case AlignToolState.WaitingForScaleConfirmation:
                _destinationPoint2 = null;
                State = AlignToolState.WaitingForDestinationPoint2;
                context.CurrentBasePoint = _destinationPoint1;
                break;
            case AlignToolState.WaitingForDestinationPoint2:
                _sourcePoint2 = null;
                _destinationPoint2 = null;
                State = AlignToolState.WaitingForSourcePoint2;
                context.CurrentBasePoint = _sourcePoint1;
                break;
            case AlignToolState.WaitingForSourcePoint2:
                _destinationPoint1 = null;
                State = AlignToolState.WaitingForDestinationPoint1;
                context.CurrentBasePoint = _sourcePoint1;
                break;
            case AlignToolState.WaitingForDestinationPoint1:
                _sourcePoint1 = null;
                State = AlignToolState.WaitingForSourcePoint1;
                context.CurrentBasePoint = null;
                break;
            default:
                return Report(ToolResult.None("Pick the first source point, or O to choose objects again."));
        }
        return Report(ToolResult.Started("Previous point cleared. " + GetPromptState(context).Prompt));
    }

    private bool EnsureState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (IsCommandFinished)
            return true;
        if (!_initialized)
        {
            _initialized = true;
            State = CaptureObjects(context) ? AlignToolState.WaitingForSourcePoint1 : AlignToolState.WaitingForEntitySelection;
        }
        if (State == AlignToolState.WaitingForEntitySelection)
        {
            PruneSelection(context);
            return true;
        }
        if (_objects.Count == context.Selection.SelectedIds.Count &&
            _objects.All(entity => context.Selection.Set.Contains(entity.Id) &&
                context.Document.Entities.TryGet(entity.Id, out CadEntity? current) &&
                current is not null && ReferenceEquals(current, entity) && context.Document.IsEntitySelectable(current)))
            return true;
        BeginObjectSelection(context);
        PruneSelection(context);
        _feedback = "Selected objects changed, disappeared or became non-editable. Review the selection and press Enter/right-click again; previous reference points were cleared.";
        return false;
    }

    private bool CaptureObjects(ToolContext context)
    {
        PruneSelection(context);
        _objects.Clear();
        _objects.AddRange(context.Document.Entities.GetByIds(context.Selection.SelectedIds));
        return _objects.Count > 0;
    }

    private static void PruneSelection(ToolContext context)
    {
        foreach (var id in context.Selection.SelectedIds.ToArray())
            if (!context.Document.Entities.TryGet(id, out CadEntity? entity) || entity is null || !context.Document.IsEntitySelectable(entity))
                context.Selection.Set.Deselect(id);
    }

    private Point2D ApplySnap(ToolContext context, Point2D point)
    {
        SnapKind kinds = context.EnabledSnaps & ~SnapKind.Entity;
        if (kinds == SnapKind.None || Tolerance.IsZero(context.SnapTolerance))
            return point;
        var request = new SnapRequest(context.Document, point, context.SnapTolerance, kinds,
            context.CurrentBasePoint, context.GridSettings);
        return context.SnapService.Snap(request)?.Point ?? point;
    }

    private ToolResult Report(ToolResult result)
    {
        _feedback = result.Message;
        return result;
    }

    public ToolResult Cancel(ToolContext context)
    {
        Reset(context);
        return ToolResult.Cancelled("Align command cancelled. The drawing is unchanged.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        Reset(context);
        return ToolResult.None("Align tool deactivated.");
    }

    private void BeginObjectSelection(ToolContext context)
    {
        Reset(context);
        _initialized = true;
        State = AlignToolState.WaitingForEntitySelection;
    }

    private void Reset(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _selectionTool.Deactivate(context);
        _objects.Clear();
        _sourcePoint1 = null;
        _destinationPoint1 = null;
        _sourcePoint2 = null;
        _destinationPoint2 = null;
        _hoverDestination1 = null;
        _translationPreview = null;
        _currentTransform = null;
        _feedback = null;
        _initialized = false;
        IsCommandFinished = false;
        State = AlignToolState.WaitingForSourcePoint1;
        context.CurrentBasePoint = null;
    }
}
