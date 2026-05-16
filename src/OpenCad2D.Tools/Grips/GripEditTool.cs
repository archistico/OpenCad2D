using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Edits the characteristic grips of one selected entity.
/// </summary>
public sealed class GripEditTool : ICadTool, IKeyboardAwareTool
{
    private readonly EntityId _entityId;
    private readonly GripProviderRegistry _registry;
    private IGripProvider? _provider;
    private CadEntity? _entity;
    private IReadOnlyList<GripPoint> _grips = Array.Empty<GripPoint>();
    private int? _hotGripIndex;
    private int? _warmGripIndex;
    private CadEntity? _previewEntity;
    private Point2D? _currentDestination;
    private bool _isInitialized;
    private bool _shouldExit;

    public GripEditTool(
        EntityId entityId,
        GripProviderRegistry registry)
    {
        _entityId = entityId;
        _registry = registry;
    }

    public string Name => "Grip Edit";

    public IReadOnlyList<GripPoint> CurrentGrips => _grips;

    public int? HotGripIndex => _hotGripIndex;

    public int? WarmGripIndex => _warmGripIndex;

    public CadEntity? PreviewEntity => _previewEntity;

    public Point2D? CurrentDestination => _currentDestination;

    public bool ShouldExit => _shouldExit;

    public ToolResult Activate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return EnsureInitialized(context);
    }

    public GripKind? ActiveGripKind => _warmGripIndex is null
        ? null
        : _grips[_warmGripIndex.Value].Kind;

    public bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (key == CadToolKey.Delete)
        {
            result = DeleteCurrentVertex(context);
            return true;
        }

        result = ToolResult.None();
        return false;
    }

    /// <summary>
    /// Deletes the currently active or highlighted polyline vertex when possible.
    /// </summary>
    public ToolResult DeleteCurrentVertex(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ToolResult initializeResult = EnsureInitialized(context);

        if (initializeResult.Changed && _shouldExit)
        {
            return initializeResult;
        }

        if (_provider is not PolylineGripProvider polylineProvider ||
            _entity is not PolylineEntity polyline)
        {
            return ToolResult.None("The active grip does not support vertex deletion.");
        }

        int? gripListIndex = _warmGripIndex ?? _hotGripIndex;

        if (gripListIndex is null)
        {
            return ToolResult.None("No polyline vertex grip is selected.");
        }

        GripPoint grip = _grips[gripListIndex.Value];

        if (!polylineProvider.CanDeleteVertex(
                polyline,
                grip.GripIndex))
        {
            return ToolResult.None("This polyline vertex cannot be deleted.");
        }

        CadEntity replacement = polylineProvider.DeleteVertex(
            polyline,
            grip.GripIndex);

        context.Commands.Execute(
            context.Document,
            new ReplaceEntitiesCommand(replacement, markDimensionsStale: true));

        _entity = context.Document.Entities.GetRequired(_entityId);
        _warmGripIndex = null;
        _previewEntity = null;
        _currentDestination = null;
        context.CurrentBasePoint = null;
        RefreshGrips();

        return ToolResult.Completed("Polyline vertex deleted.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        ToolResult initializeResult = EnsureInitialized(context);

        if (initializeResult.Changed && _shouldExit)
        {
            return initializeResult;
        }

        if (_warmGripIndex is null)
        {
            int? gripIndex = FindGripIndexNear(
                pointer.ModelPoint,
                context.Selection.Tolerance);

            if (gripIndex is null)
            {
                return ToolResult.None("No grip selected.");
            }

            _warmGripIndex = gripIndex.Value;
            _hotGripIndex = gripIndex.Value;
            _previewEntity = null;
            _currentDestination = null;
            context.CurrentBasePoint = _grips[gripIndex.Value].Position;

            return ToolResult.Started("Grip selected. Specify destination point, type coordinates, or type distance.");
        }

        Point2D destination = ResolveDestinationPoint(
            context,
            pointer.ModelPoint);

        return CommitGripMove(
            context,
            destination);
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        ToolResult initializeResult = EnsureInitialized(context);

        if (initializeResult.Changed && _shouldExit)
        {
            return initializeResult;
        }

        if (_warmGripIndex is null)
        {
            _hotGripIndex = FindGripIndexNear(
                pointer.ModelPoint,
                context.Selection.Tolerance);

            return _hotGripIndex is null
                ? ToolResult.None()
                : ToolResult.Updated("Grip highlighted.");
        }

        Point2D destination = ResolveDestinationPoint(
            context,
            pointer.ModelPoint);

        UpdatePreview(destination);

        return ToolResult.Updated("Grip preview updated.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_warmGripIndex is not null)
        {
            _warmGripIndex = null;
            _previewEntity = null;
            _currentDestination = null;
            context.CurrentBasePoint = null;

            return ToolResult.Cancelled("Grip edit cancelled. Grips remain active.");
        }

        _shouldExit = true;
        context.CurrentBasePoint = null;

        return ToolResult.Cancelled("Grip edit exited.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _hotGripIndex = null;
        _warmGripIndex = null;
        _previewEntity = null;
        _currentDestination = null;
        context.CurrentBasePoint = null;

        return ToolResult.None("Grip edit deactivated.");
    }

    private ToolResult EnsureInitialized(ToolContext context)
    {
        if (_isInitialized)
        {
            return ToolResult.None();
        }

        _isInitialized = true;

        if (!context.Document.Entities.TryGet(_entityId, out CadEntity? entity) ||
            entity is null)
        {
            _shouldExit = true;
            return ToolResult.Cancelled("Cannot enter grip edit mode because the entity was not found.");
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            _shouldExit = true;
            return ToolResult.Cancelled("Cannot enter grip edit mode because the entity is not selectable.");
        }

        _provider = _registry.FindProvider(entity);

        if (_provider is null)
        {
            _shouldExit = true;
            return ToolResult.Cancelled("Selected entity does not support grip editing.");
        }

        _entity = entity;
        RefreshGrips();

        return ToolResult.Started("Grip edit started.");
    }

    private int? FindGripIndexNear(
        Point2D point,
        double tolerance)
    {
        int? bestIndex = null;
        double bestDistance = double.MaxValue;

        for (int i = 0; i < _grips.Count; i++)
        {
            double distance = point.DistanceTo(_grips[i].Position);

            if (distance > tolerance || distance >= bestDistance)
            {
                continue;
            }

            bestIndex = i;
            bestDistance = distance;
        }

        return bestIndex;
    }

    private Point2D ResolveDestinationPoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        if (_warmGripIndex is null)
        {
            return cursorPoint;
        }

        GripPoint warmGrip = _grips[_warmGripIndex.Value];
        Point2D destination = ApplySnap(
            context,
            cursorPoint,
            warmGrip.Position);

        if (warmGrip.Kind != GripKind.ResizeRadius)
        {
            destination = ToolInputConstraintService.ApplyAngleConstraint(
                context,
                warmGrip.Position,
                destination);
        }

        return destination;
    }

    private Point2D ApplySnap(
        ToolContext context,
        Point2D cursorPoint,
        Point2D basePoint)
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

    private ToolResult CommitGripMove(
        ToolContext context,
        Point2D destination)
    {
        if (_provider is null ||
            _entity is null ||
            _warmGripIndex is null)
        {
            return ToolResult.None("No active grip to edit.");
        }

        CadEntity replacement = _provider.ApplyGripMove(
            _entity,
            _grips[_warmGripIndex.Value].GripIndex,
            destination);

        context.Commands.Execute(
            context.Document,
            new ReplaceEntitiesCommand(replacement, markDimensionsStale: true));

        _entity = context.Document.Entities.GetRequired(_entityId);
        _warmGripIndex = null;
        _previewEntity = null;
        _currentDestination = null;
        context.CurrentBasePoint = null;
        RefreshGrips();

        return ToolResult.Completed("Grip edit completed.");
    }

    private void UpdatePreview(Point2D destination)
    {
        if (_provider is null ||
            _entity is null ||
            _warmGripIndex is null)
        {
            _previewEntity = null;
            _currentDestination = null;
            return;
        }

        _currentDestination = destination;
        _previewEntity = _provider.ApplyGripMove(
            _entity,
            _grips[_warmGripIndex.Value].GripIndex,
            destination);
    }

    private void RefreshGrips()
    {
        if (_provider is null || _entity is null)
        {
            _grips = Array.Empty<GripPoint>();
            return;
        }

        _grips = _provider.GetGrips(_entity);
        _hotGripIndex = null;
    }
}
