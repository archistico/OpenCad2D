using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw ellipse entities by center, major axis point and minor radius point.
/// </summary>
public sealed class EllipseTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider, IToolPreviewDescriptorProvider
{
    public string Name => "Ellipse";

    public EllipseToolState State { get; private set; } = EllipseToolState.WaitingForCenter;

    public Point2D? Center { get; private set; }

    public Point2D? MajorAxisPoint { get; private set; }

    public Point2D? CurrentPoint { get; private set; }

    public bool HasPreview => Center is not null && CurrentPoint is not null;

    public ToolResult OnPointerPressed(ToolContext context, PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(context);


        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint);

        return SubmitPoint(context, point);
    }

    public ToolResult OnPointerMoved(ToolContext context, PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(context);

        if (State == EllipseToolState.WaitingForCenter)
        {
            return ToolResult.None();
        }

        CurrentPoint = ResolveInputPoint(
            context,
            pointer.ModelPoint);

        return ToolResult.Updated();
    }

    public ToolResult OnPointerReleased(ToolContext context, PointerInfo pointer)
    {
        return ToolResult.None();
    }

    public ToolResult Cancel(ToolContext context)
    {
        Reset();
        context.CurrentBasePoint = null;
        return ToolResult.Cancelled("Ellipse cancelled.");
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            EllipseToolState.WaitingForCenter => new CommandPromptState(
                "ELLIPSE",
                "Specify center point",
                CommandInputKind.Point,
                placeholder: "100,50"),

            EllipseToolState.WaitingForMajorAxis => new CommandPromptState(
                "ELLIPSE",
                "Specify major axis endpoint",
                CommandInputKind.Point,
                placeholder: "150,50   |   @50,0   |   @100<45"),

            _ => new CommandPromptState(
                "ELLIPSE",
                "Specify minor axis radius point or type radius",
                CommandInputKind.PointOrDistance,
                placeholder: "150,75   |   @0,25   |   radius")
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "ELLIPSE expects a point or radius input.");
        }

        return SubmitPoint(context, input.Point.Value);
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EllipseEntity? preview = GetPreviewEntity();
        return preview is null
            ? Array.Empty<CadEntity>()
            : new CadEntity[] { preview };
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entities = new List<CadEntity>();
        var lines = new List<ToolPreviewLine>();
        var markers = new List<ToolPreviewMarker>();

        EllipseEntity? preview = GetPreviewEntity();
        if (preview is not null)
        {
            entities.Add(preview);
        }

        if (Center is null)
        {
            return new ToolPreviewDescriptor(entities: entities);
        }

        markers.Add(new ToolPreviewMarker(
            Center.Value,
            ToolPreviewMarkerKind.Primary));

        if (State == EllipseToolState.WaitingForMajorAxis)
        {
            AddAxisPreview(
                lines,
                markers,
                Center.Value,
                CurrentPoint);
        }
        else if (MajorAxisPoint is not null)
        {
            Vector2D majorAxis = Center.Value.VectorTo(MajorAxisPoint.Value);

            if (majorAxis.Length > 0)
            {
                lines.Add(new ToolPreviewLine(
                    Center.Value - majorAxis,
                    Center.Value + majorAxis,
                    ToolPreviewLineKind.Axis));
            }

            markers.Add(new ToolPreviewMarker(
                MajorAxisPoint.Value,
                ToolPreviewMarkerKind.Secondary));

            AddAxisPreview(
                lines,
                markers,
                Center.Value,
                GetMinorAxisPreviewPoint(CurrentPoint));
        }

        return new ToolPreviewDescriptor(
            entities: entities,
            lines: lines,
            markers: markers);
    }

    public EllipseEntity? GetPreviewEntity()
    {
        if (Center is null || CurrentPoint is null)
        {
            return null;
        }

        if (State == EllipseToolState.WaitingForMajorAxis)
        {
            Vector2D previewMajorAxis = Center.Value.VectorTo(CurrentPoint.Value);
            if (previewMajorAxis.Length <= 0)
            {
                return null;
            }

            return new EllipseEntity(
                Center.Value,
                previewMajorAxis,
                Math.Max(previewMajorAxis.Length * 0.5, 0.0001));
        }

        if (MajorAxisPoint is null)
        {
            return null;
        }

        Vector2D majorAxis = Center.Value.VectorTo(MajorAxisPoint.Value);
        double minorRadius = GetMinorRadius(CurrentPoint.Value);
        if (majorAxis.Length <= 0 || minorRadius <= 0)
        {
            return null;
        }

        return new EllipseEntity(
            Center.Value,
            majorAxis,
            minorRadius);
    }

    private ToolResult SubmitPoint(
        ToolContext context,
        Point2D point)
    {
        switch (State)
        {
            case EllipseToolState.WaitingForCenter:
                Center = point;
                CurrentPoint = point;
                context.CurrentBasePoint = point;
                State = EllipseToolState.WaitingForMajorAxis;
                return ToolResult.Started("Specify major axis endpoint.");

            case EllipseToolState.WaitingForMajorAxis:
                if (Center is null)
                {
                    Reset();
                    return ToolResult.None("Ellipse center is missing.");
                }

                if (context.GeometryTolerance.AreDistancesEqual(Center.Value.DistanceTo(point), 0))
                {
                    return ToolResult.None("Ellipse major radius must be greater than zero.");
                }

                MajorAxisPoint = point;
                CurrentPoint = point;
                context.CurrentBasePoint = Center.Value;
                State = EllipseToolState.WaitingForMinorRadius;
                return ToolResult.Started("Specify minor axis radius point or type radius.");

            case EllipseToolState.WaitingForMinorRadius:
                return Complete(context, point);

            default:
                return ToolResult.None();
        }
    }

    private ToolResult Complete(
        ToolContext context,
        Point2D point)
    {
        if (Center is null || MajorAxisPoint is null)
        {
            Reset();
            return ToolResult.None("Ellipse definition is incomplete.");
        }

        Vector2D majorAxis = Center.Value.VectorTo(MajorAxisPoint.Value);
        double minorRadius = GetMinorRadius(point);

        if (context.GeometryTolerance.AreDistancesEqual(minorRadius, 0))
        {
            return ToolResult.None("Ellipse minor radius must be greater than zero.");
        }

        var ellipse = new EllipseEntity(
            Center.Value,
            majorAxis,
            minorRadius,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(ellipse));

        Reset();
        context.CurrentBasePoint = null;
        return ToolResult.Completed("Ellipse created.");
    }

    private Point2D ResolveInputPoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        Point2D? basePoint = State switch
        {
            EllipseToolState.WaitingForMajorAxis => Center,
            EllipseToolState.WaitingForMinorRadius => Center,
            _ => context.CurrentBasePoint
        };

        Point2D point = ApplySnap(
            context,
            cursorPoint,
            basePoint);

        if (State == EllipseToolState.WaitingForMajorAxis &&
            basePoint is not null)
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

    private double GetMinorRadius(Point2D point)
    {
        if (Center is null || MajorAxisPoint is null)
        {
            return 0;
        }

        Vector2D majorAxis = Center.Value.VectorTo(MajorAxisPoint.Value);
        Vector2D majorDirection = majorAxis.Normalize();
        Vector2D centerToPoint = Center.Value.VectorTo(point);
        double projection = centerToPoint.Dot(majorDirection);
        Vector2D perpendicular = centerToPoint - majorDirection * projection;

        return perpendicular.Length;
    }

    private Point2D? GetMinorAxisPreviewPoint(Point2D? point)
    {
        if (Center is null ||
            MajorAxisPoint is null ||
            point is null)
        {
            return null;
        }

        Vector2D majorAxis = Center.Value.VectorTo(MajorAxisPoint.Value);

        if (majorAxis.Length <= 0)
        {
            return null;
        }

        Vector2D minorDirection = majorAxis.Normalize().PerpendicularLeft();
        double signedMinorRadius = Center.Value.VectorTo(point.Value).Dot(minorDirection);

        if (Tolerance.IsZero(signedMinorRadius))
        {
            return null;
        }

        return Center.Value + minorDirection * signedMinorRadius;
    }

    private static void AddAxisPreview(
        ICollection<ToolPreviewLine> lines,
        ICollection<ToolPreviewMarker> markers,
        Point2D start,
        Point2D? end)
    {
        if (end is null ||
            Tolerance.ArePointsEqual(start, end.Value))
        {
            return;
        }

        lines.Add(new ToolPreviewLine(
            start,
            end.Value,
            ToolPreviewLineKind.Axis));

        markers.Add(new ToolPreviewMarker(
            end.Value,
            ToolPreviewMarkerKind.Secondary));
    }

    private void Reset()
    {
        State = EllipseToolState.WaitingForCenter;
        Center = null;
        MajorAxisPoint = null;
        CurrentPoint = null;
    }
}
