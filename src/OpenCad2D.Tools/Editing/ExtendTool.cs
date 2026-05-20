using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Extends editable entities until they reach a selected boundary entity.
/// </summary>
public sealed class ExtendTool : ICadTool, ICommandDrivenTool, IToolPreviewDescriptorProvider, ISnapModeProvider
{
    private EntityId? _boundaryEntityId;
    private CadEntity? _boundaryEntity;
    private EntityId? _previewTargetEntityId;
    private CadEntity? _previewEntity;
    private IReadOnlyList<CadEntity> _highlightPreviewEntities = Array.Empty<CadEntity>();

    public string Name => "Extend";

    public ExtendToolState State { get; private set; } =
        ExtendToolState.WaitingForBoundaryEntity;

    public EntityId? BoundaryEntityId => _boundaryEntityId;

    public bool HasPreview =>
        State == ExtendToolState.WaitingForTargetEntity &&
        _previewEntity is not null;


    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            ExtendToolState.WaitingForBoundaryEntity => new CommandPromptState(
                "EXTEND",
                "Select boundary entity",
                CommandInputKind.Selection,
                placeholder: "Click a line, circle, arc, ellipse, elliptical arc or polyline boundary"),

            ExtendToolState.WaitingForTargetEntity => new CommandPromptState(
                "EXTEND",
                "Select entity to extend",
                CommandInputKind.Selection,
                placeholder: "Click the endpoint side to extend"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        return State == ExtendToolState.WaitingForBoundaryEntity
            ? ToolResult.None("Select a boundary entity from the drawing canvas.")
            : ToolResult.None("Select the endpoint side to extend from the drawing canvas.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            ExtendToolState.WaitingForBoundaryEntity =>
                AcceptBoundaryEntity(context, pointer),

            ExtendToolState.WaitingForTargetEntity =>
                AcceptTargetEntity(context, pointer),

            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != ExtendToolState.WaitingForTargetEntity ||
            _boundaryEntity is null)
        {
            return ToolResult.None();
        }

        UpdatePreview(context, pointer.ModelPoint);

        return HasPreview
            ? ToolResult.Updated("Extend preview updated. Highlighted portion will be added.")
            : ToolResult.None("Select an extendable endpoint that reaches the boundary from the picked side.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Extend command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Extend tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewEntity is null
            ? Array.Empty<CadEntity>()
            : new[] { _previewEntity };
    }

    /// <summary>
    /// Gets the entities that represent the new portion added by the current extend preview.
    /// </summary>
    public IReadOnlyList<CadEntity> GetHighlightedPreviewEntities()
    {
        return _highlightPreviewEntities.ToList();
    }


    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ToolPreviewDescriptor(
            entities: GetPreviewEntities(),
            highlightedEntities: GetHighlightedPreviewEntities(),
            highlightedEntityKind: ToolPreviewHighlightKind.Addition,
            entityOverlays: GetSelectedBoundaryOverlays());
    }

    private IReadOnlyList<ToolPreviewEntityOverlay> GetSelectedBoundaryOverlays()
    {
        if (_boundaryEntity is null)
        {
            return Array.Empty<ToolPreviewEntityOverlay>();
        }

        return new[]
        {
            new ToolPreviewEntityOverlay(
                new[] { _boundaryEntity },
                ToolPreviewHighlightKind.Emphasis)
        };
    }

    private ToolResult AcceptBoundaryEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        EntityId? selectedId = SelectVisibleEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select a visible boundary entity.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedBoundaryEntity(entity))
        {
            return ToolResult.None("Extend supports lines, circles, arcs, ellipses, elliptical arcs and polylines as boundaries.");
        }

        if (!context.Document.IsEntityVisible(entity))
        {
            return ToolResult.None("Boundary entity is not visible.");
        }

        _boundaryEntityId = entity.Id;
        _boundaryEntity = entity;
        _previewTargetEntityId = null;
        _previewEntity = null;
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        State = ExtendToolState.WaitingForTargetEntity;
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Started(
            "Select the endpoint side to extend. Highlighted preview shows the portion that will be added.");
    }

    private ToolResult AcceptTargetEntity(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_boundaryEntity is null)
        {
            throw new InvalidOperationException(
                "Cannot extend before selecting a boundary entity.");
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            pointer.ModelPoint);

        if (selectedId is null)
        {
            return ToolResult.None("Select an entity to extend.");
        }

        if (selectedId.Value.Equals(_boundaryEntity.Id))
        {
            return ToolResult.None("Target entity must be different from the boundary entity.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedTargetEntity(entity))
        {
            return ToolResult.None("Extend supports lines, arcs, elliptical arcs and open polylines as targets. Closed curves cannot be extended.");
        }

        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        CadEntity? extendedEntity = CadExtendService.ExtendToBoundary(
            entity,
            _boundaryEntity,
            pointer.ModelPoint,
            context.GeometryTolerance);

        if (extendedEntity is null)
        {
            return ToolResult.None(
                "No valid extension reaches the boundary from the picked endpoint side.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { entity },
                new[] { extendedEntity },
                "Extend entity"));

        _previewTargetEntityId = null;
        _previewEntity = null;
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        context.CurrentBasePoint = entity.GetClosestPoint(pointer.ModelPoint);

        return ToolResult.Completed(
            "Entity extended. Select another endpoint side to extend, or press Escape.");
    }

    private void UpdatePreview(
        ToolContext context,
        Point2D point)
    {
        if (_boundaryEntity is null)
        {
            _previewTargetEntityId = null;
            _previewEntity = null;
            _highlightPreviewEntities = Array.Empty<CadEntity>();
            return;
        }

        EntityId? selectedId = SelectEntityByPoint(
            context,
            point);

        if (selectedId is null ||
            selectedId.Value.Equals(_boundaryEntity.Id))
        {
            _previewTargetEntityId = null;
            _previewEntity = null;
            _highlightPreviewEntities = Array.Empty<CadEntity>();
            return;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        if (!IsSupportedTargetEntity(entity) ||
            !context.Document.IsEntitySelectable(entity))
        {
            _previewTargetEntityId = null;
            _previewEntity = null;
            _highlightPreviewEntities = Array.Empty<CadEntity>();
            return;
        }

        _previewTargetEntityId = entity.Id;
        _previewEntity = CadExtendService.ExtendToBoundary(
            entity,
            _boundaryEntity,
            point,
            context.GeometryTolerance);
        _highlightPreviewEntities = _previewEntity is null
            ? Array.Empty<CadEntity>()
            : CreateExtendHighlightEntities(
                entity,
                _previewEntity,
                context.GeometryTolerance);
    }


    private static IReadOnlyList<CadEntity> CreateExtendHighlightEntities(
        CadEntity source,
        CadEntity extended,
        GeometryTolerance tolerance)
    {
        return (source, extended) switch
        {
            (LineEntity sourceLine, LineEntity extendedLine) =>
                CreateLineExtendHighlightEntities(sourceLine, extendedLine, tolerance),

            (ArcEntity sourceArc, ArcEntity extendedArc) =>
                CreateArcExtendHighlightEntities(sourceArc, extendedArc, tolerance),

            (EllipticalArcEntity sourceArc, EllipticalArcEntity extendedArc) =>
                CreateEllipticalArcExtendHighlightEntities(sourceArc, extendedArc, tolerance),

            (PolylineEntity sourcePolyline, PolylineEntity extendedPolyline) =>
                CreatePolylineExtendHighlightEntities(sourcePolyline, extendedPolyline, tolerance),

            _ => Array.Empty<CadEntity>()
        };
    }

    private static IReadOnlyList<CadEntity> CreateLineExtendHighlightEntities(
        LineEntity sourceLine,
        LineEntity extendedLine,
        GeometryTolerance tolerance)
    {
        if (!tolerance.ArePointsEqual(sourceLine.Start, extendedLine.Start))
        {
            return CreateHighlightLine(
                sourceLine,
                extendedLine.Start,
                sourceLine.Start,
                tolerance);
        }

        if (!tolerance.ArePointsEqual(sourceLine.End, extendedLine.End))
        {
            return CreateHighlightLine(
                sourceLine,
                sourceLine.End,
                extendedLine.End,
                tolerance);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreateArcExtendHighlightEntities(
        ArcEntity sourceArc,
        ArcEntity extendedArc,
        GeometryTolerance tolerance)
    {
        if (!tolerance.ArePointsEqual(
                sourceArc.Geometry.StartPoint,
                extendedArc.Geometry.StartPoint))
        {
            return CreateHighlightArc(
                sourceArc,
                extendedArc.StartAngle,
                sourceArc.StartAngle);
        }

        if (!tolerance.ArePointsEqual(
                sourceArc.Geometry.EndPoint,
                extendedArc.Geometry.EndPoint))
        {
            return CreateHighlightArc(
                sourceArc,
                sourceArc.EndAngle,
                extendedArc.EndAngle);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreateEllipticalArcExtendHighlightEntities(
        EllipticalArcEntity sourceArc,
        EllipticalArcEntity extendedArc,
        GeometryTolerance tolerance)
    {
        if (!tolerance.ArePointsEqual(sourceArc.StartPoint, extendedArc.StartPoint))
        {
            return CreateHighlightEllipticalArc(
                sourceArc,
                extendedArc.StartParameterRadians,
                sourceArc.StartParameterRadians);
        }

        if (!tolerance.ArePointsEqual(sourceArc.EndPoint, extendedArc.EndPoint))
        {
            return CreateHighlightEllipticalArc(
                sourceArc,
                sourceArc.EndParameterRadians,
                extendedArc.EndParameterRadians);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreatePolylineExtendHighlightEntities(
        PolylineEntity sourcePolyline,
        PolylineEntity extendedPolyline,
        GeometryTolerance tolerance)
    {
        if (sourcePolyline.IsClosed ||
            extendedPolyline.IsClosed ||
            sourcePolyline.Vertices.Count < 2 ||
            extendedPolyline.Vertices.Count < 2)
        {
            return Array.Empty<CadEntity>();
        }

        if (!tolerance.ArePointsEqual(
                sourcePolyline.Vertices[0],
                extendedPolyline.Vertices[0]))
        {
            return CreateHighlightLine(
                sourcePolyline,
                extendedPolyline.Vertices[0],
                sourcePolyline.Vertices[0],
                tolerance);
        }

        if (!tolerance.ArePointsEqual(
                sourcePolyline.Vertices[^1],
                extendedPolyline.Vertices[^1]))
        {
            return CreateHighlightLine(
                sourcePolyline,
                sourcePolyline.Vertices[^1],
                extendedPolyline.Vertices[^1],
                tolerance);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CadEntity> CreateHighlightArc(
        ArcEntity source,
        Angle startAngle,
        Angle endAngle)
    {
        return new[]
        {
            new ArcEntity(
                source.Center,
                source.Radius,
                startAngle,
                endAngle,
                source.IsCounterClockwise,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
        };
    }

    private static IReadOnlyList<CadEntity> CreateHighlightEllipticalArc(
        EllipticalArcEntity source,
        double startParameterRadians,
        double endParameterRadians)
    {
        return new[]
        {
            new EllipticalArcEntity(
                source.Center,
                source.MajorAxis,
                source.MinorRadius,
                startParameterRadians,
                endParameterRadians,
                source.IsCounterClockwise,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
        };
    }

    private static IReadOnlyList<CadEntity> CreateHighlightLine(
        CadEntity source,
        Point2D start,
        Point2D end,
        GeometryTolerance tolerance)
    {
        if (start.DistanceTo(end) <= tolerance.Distance)
        {
            return Array.Empty<CadEntity>();
        }

        return new[]
        {
            new LineEntity(
                start,
                end,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
        };
    }


    private static EntityId? SelectVisibleEntityByPoint(
        ToolContext context,
        Point2D point)
    {
        BoundingBox2D searchArea = new(
            point.X - context.Selection.Tolerance,
            point.Y - context.Selection.Tolerance,
            point.X + context.Selection.Tolerance,
            point.Y + context.Selection.Tolerance);

        return context.Document.GetVisibleEntities(searchArea)
            .Select(entity => new
            {
                Entity = entity,
                Distance = point.DistanceTo(entity.GetClosestPoint(point))
            })
            .Where(result => result.Distance <= context.Selection.Tolerance)
            .OrderBy(result => result.Distance)
            .ThenByDescending(result => result.Entity.DrawOrder)
            .Select(result => (EntityId?)result.Entity.Id)
            .FirstOrDefault();
    }

    private static EntityId? SelectEntityByPoint(
        ToolContext context,
        Point2D point)
    {
        return context.Selection.Service.SelectByPoint(
            context.Document,
            point,
            context.Selection.Tolerance);
    }


    private static bool IsSupportedBoundaryEntity(CadEntity entity)
    {
        return entity is LineEntity or CircleEntity or ArcEntity or EllipseEntity or EllipticalArcEntity or PolylineEntity;
    }

    private static bool IsSupportedTargetEntity(CadEntity entity)
    {
        return entity is LineEntity or ArcEntity or EllipticalArcEntity or PolylineEntity { IsClosed: false };
    }

    private void Reset(ToolContext? context = null)
    {
        _boundaryEntityId = null;
        _boundaryEntity = null;
        _previewTargetEntityId = null;
        _previewEntity = null;
        _highlightPreviewEntities = Array.Empty<CadEntity>();
        State = ExtendToolState.WaitingForBoundaryEntity;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
