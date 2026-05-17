using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw regular polygons as closed polylines.
/// </summary>
public sealed class PolygonTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider
{
    public const int DefaultSideCount = 6;
    public const int MinimumSideCount = 3;
    public const int MaximumSideCount = 256;

    private int _sideCount = DefaultSideCount;
    private Point2D? _center;
    private Point2D? _currentVertex;

    public string Name => "Polygon";

    public PolygonToolState State { get; private set; } = PolygonToolState.WaitingForSides;

    public int SideCount => _sideCount;

    public Point2D? Center => _center;

    public Point2D? CurrentVertex => _currentVertex;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            PolygonToolState.WaitingForSides => new CommandPromptState(
                "POLYGON",
                $"Enter number of sides <{DefaultSideCount}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "3-256"),

            PolygonToolState.WaitingForCenter => new CommandPromptState(
                "POLYGON",
                "Specify center point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            PolygonToolState.WaitingForVertex => new CommandPromptState(
                "POLYGON",
                "Specify vertex point or type radius",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   @100<45   |   radius"),

            _ => new CommandPromptState(
                "POLYGON",
                "Enter number of sides",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "3-256")
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == PolygonToolState.WaitingForSides)
        {
            if (input.Kind == CommandInputSubmissionKind.Confirm)
            {
                return AcceptSideCount(context, DefaultSideCount);
            }

            if (input.Kind != CommandInputSubmissionKind.Number || input.Number is null)
            {
                return ToolResult.None(input.ErrorMessage ?? "POLYGON expects a number of sides.");
            }

            return AcceptSideCount(context, input.Number.Value);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "POLYGON expects a point input.");
        }

        return SubmitResolvedPoint(context, input.Point.Value);
    }

    public bool HasPreview =>
        State == PolygonToolState.WaitingForVertex &&
        _center is not null &&
        _currentVertex is not null &&
        CanCreatePolygon(_center.Value, _currentVertex.Value, contextTolerance: GeometryTolerance.Default);

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        PolylineEntity? preview = GetPreviewEntity();
        return preview is null
            ? Array.Empty<CadEntity>()
            : new CadEntity[] { preview };
    }

    public PolylineEntity? GetPreviewEntity()
    {
        if (_center is null || _currentVertex is null)
        {
            return null;
        }

        if (!CanCreatePolygon(
                _center.Value,
                _currentVertex.Value,
                GeometryTolerance.Default))
        {
            return null;
        }

        return CreatePolygonEntity(
            _center.Value,
            _currentVertex.Value,
            _sideCount);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State == PolygonToolState.WaitingForSides)
        {
            return ToolResult.None("Enter the number of polygon sides first.");
        }

        Point2D point = ResolvePoint(context, pointer.ModelPoint);

        return SubmitResolvedPoint(context, point);
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != PolygonToolState.WaitingForVertex || _center is null)
        {
            return ToolResult.None();
        }

        _currentVertex = ResolvePoint(context, pointer.ModelPoint);

        return ToolResult.Updated();
    }

    public ToolResult OnPointerReleased(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return ToolResult.None();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Polygon command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Polygon tool deactivated.");
    }

    private ToolResult AcceptSideCount(
        ToolContext context,
        double sideCountValue)
    {
        if (!TryNormalizeSideCount(sideCountValue, out int sideCount, out string? errorMessage))
        {
            return ToolResult.None(errorMessage);
        }

        _sideCount = sideCount;
        State = PolygonToolState.WaitingForCenter;
        context.CurrentBasePoint = null;

        return ToolResult.Started("Specify polygon center point.");
    }

    private ToolResult SubmitResolvedPoint(
        ToolContext context,
        Point2D point)
    {
        if (State == PolygonToolState.WaitingForCenter)
        {
            _center = point;
            _currentVertex = point;
            State = PolygonToolState.WaitingForVertex;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Specify polygon vertex point or type radius.");
        }

        if (State == PolygonToolState.WaitingForVertex)
        {
            if (_center is null)
            {
                return ToolResult.None("Polygon center point is missing.");
            }

            if (!CanCreatePolygon(
                    _center.Value,
                    point,
                    context.GeometryTolerance))
            {
                return ToolResult.None("Polygon radius must be greater than zero.");
            }

            PolylineEntity polygon = CreatePolygonEntity(
                _center.Value,
                point,
                _sideCount,
                context.Creation.CurrentLayerId);

            context.Commands.Execute(
                context.Document,
                new AddEntityCommand(polygon));

            Reset(context);

            return ToolResult.Completed("Polygon created.");
        }

        return ToolResult.None();
    }

    private Point2D ResolvePoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        Point2D? basePoint = State == PolygonToolState.WaitingForVertex
            ? _center
            : null;

        Point2D point = ApplySnap(
            context,
            cursorPoint,
            basePoint);

        if (basePoint is not null)
        {
            point = ToolInputConstraintService.ApplyAngleConstraint(
                context,
                basePoint.Value,
                point);
        }

        return point;
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

    private void Reset(ToolContext context)
    {
        _sideCount = DefaultSideCount;
        _center = null;
        _currentVertex = null;
        State = PolygonToolState.WaitingForSides;
        context.CurrentBasePoint = null;
    }

    private static bool TryNormalizeSideCount(
        double sideCountValue,
        out int sideCount,
        out string? errorMessage)
    {
        sideCount = 0;
        errorMessage = null;

        double rounded = Math.Round(sideCountValue);
        if (!Tolerance.AreEqual(sideCountValue, rounded))
        {
            errorMessage = "Polygon side count must be an integer.";
            return false;
        }

        sideCount = (int)rounded;

        if (sideCount < MinimumSideCount || sideCount > MaximumSideCount)
        {
            errorMessage = $"Polygon side count must be between {MinimumSideCount} and {MaximumSideCount}.";
            return false;
        }

        return true;
    }

    private static bool CanCreatePolygon(
        Point2D center,
        Point2D vertex,
        GeometryTolerance contextTolerance)
    {
        return !contextTolerance.AreDistancesEqual(
            center.DistanceTo(vertex),
            0);
    }

    private static PolylineEntity CreatePolygonEntity(
        Point2D center,
        Point2D vertex,
        int sideCount,
        LayerId? layerId = null)
    {
        double radius = center.DistanceTo(vertex);
        double startAngle = Math.Atan2(
            vertex.Y - center.Y,
            vertex.X - center.X);
        double step = 2.0 * Math.PI / sideCount;

        List<Point2D> vertices = new(sideCount);
        for (int index = 0; index < sideCount; index++)
        {
            double angle = startAngle + index * step;
            vertices.Add(new Point2D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle)));
        }

        return new PolylineEntity(
            vertices,
            isClosed: true,
            layerId: layerId ?? LayerId.Default);
    }
}
