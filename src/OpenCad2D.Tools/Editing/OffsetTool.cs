using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates parallel/constant-distance copies of supported entities.
/// v0.8 intentionally supports lines, circles and arcs; polyline offset is deferred because robust joins
/// require a dedicated topology service.
/// </summary>
public sealed class OffsetTool : ICadTool, ICommandDrivenTool
{
    private double? _distance;
    private ToolPickedEntityInput? _pickedEntity;

    public string Name => "Offset";

    public OffsetToolState State { get; private set; } = OffsetToolState.WaitingForDistance;

    public double? Distance => _distance;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            OffsetToolState.WaitingForDistance => new CommandPromptState(
                "OFFSET",
                "Specify offset distance",
                CommandInputKind.Distance,
                placeholder: "Distance, for example 100"),

            OffsetToolState.WaitingForEntity => new CommandPromptState(
                "OFFSET",
                "Select object to offset",
                CommandInputKind.Selection,
                placeholder: "Click line, circle or arc"),

            OffsetToolState.WaitingForSidePoint => new CommandPromptState(
                "OFFSET",
                "Specify side to offset",
                CommandInputKind.Point,
                placeholder: "Click side or type a point"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == OffsetToolState.WaitingForDistance &&
            input.Kind == CommandInputSubmissionKind.Distance &&
            input.Distance is not null)
        {
            return AcceptDistance(context, input.Distance.Value);
        }

        if (State == OffsetToolState.WaitingForSidePoint &&
            input.Kind == CommandInputSubmissionKind.Point &&
            input.Point is not null)
        {
            return CreateOffset(context, input.Point.Value);
        }

        return State switch
        {
            OffsetToolState.WaitingForDistance => ToolResult.None("Specify a positive offset distance."),
            OffsetToolState.WaitingForEntity => ToolResult.None("Select a line, circle or arc from the drawing canvas."),
            OffsetToolState.WaitingForSidePoint => ToolResult.None("Specify the side to offset by clicking or typing a point."),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            OffsetToolState.WaitingForDistance => ToolResult.None("Type the offset distance in the command input."),
            OffsetToolState.WaitingForEntity => AcceptEntity(context, pointer.ModelPoint),
            OffsetToolState.WaitingForSidePoint => CreateOffset(context, pointer.ModelPoint),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
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
        return ToolResult.Cancelled("Offset command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.None("Offset tool deactivated.");
    }

    private ToolResult AcceptDistance(
        ToolContext context,
        double distance)
    {
        if (distance <= 0 || context.GeometryTolerance.IsDistanceZero(distance))
        {
            return ToolResult.None("Offset distance must be greater than zero.");
        }

        _distance = distance;
        _pickedEntity = null;
        State = OffsetToolState.WaitingForEntity;
        context.CurrentBasePoint = null;

        return ToolResult.Started("Select object to offset.");
    }

    private ToolResult AcceptEntity(
        ToolContext context,
        Point2D pickPoint)
    {
        ToolPickedEntityInput? picked = PickSelectableEntity(context, pickPoint);

        if (picked is null)
        {
            return ToolResult.None("Select a visible, unlocked line, circle or arc to offset.");
        }

        if (!IsSupportedEntity(picked.Entity))
        {
            return ToolResult.None("Offset currently supports lines, circles and arcs.");
        }

        _pickedEntity = picked;
        State = OffsetToolState.WaitingForSidePoint;
        context.CurrentBasePoint = picked.ClosestPoint;

        return ToolResult.Started("Specify side to offset.");
    }

    private ToolResult CreateOffset(
        ToolContext context,
        Point2D sidePoint)
    {
        if (_distance is null)
        {
            State = OffsetToolState.WaitingForDistance;
            return ToolResult.None("Specify offset distance first.");
        }

        if (_pickedEntity is null)
        {
            State = OffsetToolState.WaitingForEntity;
            return ToolResult.None("Select object to offset first.");
        }

        if (!TryCreateOffsetEntity(
                _pickedEntity.Entity,
                sidePoint,
                _distance.Value,
                context.GeometryTolerance,
                out CadEntity? offsetEntity,
                out string? errorMessage))
        {
            return ToolResult.None(errorMessage ?? "Cannot offset selected entity.");
        }

        if (offsetEntity is null)
        {
            return ToolResult.None(errorMessage ?? "Cannot offset selected entity.");
        }

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(offsetEntity));

        _pickedEntity = null;
        State = OffsetToolState.WaitingForEntity;
        context.CurrentBasePoint = null;

        return ToolResult.Completed("Offset entity created. Select another object to offset or press Escape.");
    }

    private static bool TryCreateOffsetEntity(
        CadEntity entity,
        Point2D sidePoint,
        double distance,
        GeometryTolerance tolerance,
        out CadEntity? offsetEntity,
        out string? errorMessage)
    {
        offsetEntity = null;
        errorMessage = null;

        switch (entity)
        {
            case LineEntity line:
                Vector2D direction = line.Start.VectorTo(line.End);
                if (tolerance.IsVectorLengthZero(direction.Length))
                {
                    errorMessage = "Cannot offset a zero-length line.";
                    return false;
                }

                Vector2D unit = direction.Normalize();
                Vector2D left = unit.PerpendicularLeft();
                double side = direction.Cross(line.Start.VectorTo(sidePoint));
                Vector2D normal = side >= 0 ? left : left * -1.0;
                Vector2D offset = normal * distance;

                offsetEntity = new LineEntity(
                    line.Start + offset,
                    line.End + offset,
                    layerId: line.LayerId,
                    style: line.Style,
                    isVisible: line.IsVisible,
                    isLocked: line.IsLocked,
                    drawOrder: line.DrawOrder + 1);
                return true;

            case CircleEntity circle:
                double circleRadius = sidePoint.DistanceTo(circle.Center) >= circle.Radius
                    ? circle.Radius + distance
                    : circle.Radius - distance;

                if (circleRadius <= tolerance.Distance)
                {
                    errorMessage = "Offset distance would make the circle radius zero or negative.";
                    return false;
                }

                offsetEntity = new CircleEntity(
                    circle.Center,
                    circleRadius,
                    layerId: circle.LayerId,
                    style: circle.Style,
                    isVisible: circle.IsVisible,
                    isLocked: circle.IsLocked,
                    drawOrder: circle.DrawOrder + 1);
                return true;

            case ArcEntity arc:
                double arcRadius = sidePoint.DistanceTo(arc.Center) >= arc.Radius
                    ? arc.Radius + distance
                    : arc.Radius - distance;

                if (arcRadius <= tolerance.Distance)
                {
                    errorMessage = "Offset distance would make the arc radius zero or negative.";
                    return false;
                }

                offsetEntity = new ArcEntity(
                    arc.Center,
                    arcRadius,
                    arc.StartAngle,
                    arc.EndAngle,
                    arc.IsCounterClockwise,
                    layerId: arc.LayerId,
                    style: arc.Style,
                    isVisible: arc.IsVisible,
                    isLocked: arc.IsLocked,
                    drawOrder: arc.DrawOrder + 1);
                return true;

            default:
                errorMessage = "Offset currently supports lines, circles and arcs.";
                return false;
        }
    }

    private static ToolPickedEntityInput? PickSelectableEntity(
        ToolContext context,
        Point2D pickPoint)
    {
        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pickPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return null;
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);

        return context.Document.IsEntitySelectable(entity)
            ? new ToolPickedEntityInput(
                selectedId.Value,
                pickPoint,
                entity.GetClosestPoint(pickPoint),
                entity)
            : null;
    }

    private static bool IsSupportedEntity(CadEntity entity)
    {
        return entity is LineEntity or CircleEntity or ArcEntity;
    }

    private void Reset(ToolContext context)
    {
        _distance = null;
        _pickedEntity = null;
        State = OffsetToolState.WaitingForDistance;
        context.CurrentBasePoint = null;
    }
}
