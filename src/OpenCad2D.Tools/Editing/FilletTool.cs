using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates a tangent fillet between two lines.
/// v0.8 supports Line-Line with Radius and Radius=0 corner joining.
/// </summary>
public sealed class FilletTool : ICadTool, ICommandDrivenTool
{
    private double _radius;
    private ToolPickedEntityInput? _firstPick;
    private IReadOnlyList<CadEntity> _previewEntities = Array.Empty<CadEntity>();

    public string Name => "Fillet";

    public FilletToolState State { get; private set; } = FilletToolState.WaitingForFirstEntityOrRadius;

    public double Radius => _radius;

    public EntityId? FirstEntityId => _firstPick?.EntityId;

    public IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        return _previewEntities;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            FilletToolState.WaitingForFirstEntityOrRadius => new CommandPromptState(
                "FILLET",
                $"Select first line or [Radius] <{_radius:0.###}>",
                CommandInputKind.SelectionOrOption,
                new[]
                {
                    new CommandOption("Radius", "R", "Set fillet radius")
                },
                placeholder: "Click first line or type R"),

            FilletToolState.WaitingForRadius => new CommandPromptState(
                "FILLET",
                "Specify fillet radius",
                CommandInputKind.Number,
                placeholder: "Radius, for example 10 or 0"),

            FilletToolState.WaitingForSecondEntity => new CommandPromptState(
                "FILLET",
                "Select second line",
                CommandInputKind.Selection,
                placeholder: "Click second line"),

            _ => CommandPromptState.Idle
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == FilletToolState.WaitingForFirstEntityOrRadius &&
            input.Kind == CommandInputSubmissionKind.Option &&
            string.Equals(input.OptionKeyword, "Radius", StringComparison.OrdinalIgnoreCase))
        {
            State = FilletToolState.WaitingForRadius;
            context.CurrentBasePoint = null;
            return ToolResult.Started("Specify fillet radius.");
        }

        if (State == FilletToolState.WaitingForRadius &&
            input.Kind == CommandInputSubmissionKind.Number &&
            input.Number is not null)
        {
            return AcceptRadius(context, input.Number.Value);
        }

        return State switch
        {
            FilletToolState.WaitingForFirstEntityOrRadius => ToolResult.None("Select the first line from the drawing canvas or type Radius."),
            FilletToolState.WaitingForRadius => ToolResult.None("Specify a non-negative fillet radius."),
            FilletToolState.WaitingForSecondEntity => ToolResult.None("Select the second line from the drawing canvas."),
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
            FilletToolState.WaitingForFirstEntityOrRadius => AcceptFirstLine(context, pointer.ModelPoint),
            FilletToolState.WaitingForRadius => ToolResult.None("Type the fillet radius in the command input."),
            FilletToolState.WaitingForSecondEntity => AcceptSecondLine(context, pointer.ModelPoint),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State != FilletToolState.WaitingForSecondEntity || _firstPick is null)
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        ToolPickedEntityInput? secondPick = PickSelectableLine(context, pointer.ModelPoint);

        if (secondPick is null || secondPick.EntityId.Equals(_firstPick.EntityId))
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        LineEntity first = (LineEntity)_firstPick.Entity;
        LineEntity second = (LineEntity)secondPick.Entity;

        if (!TryCreateLineLineFillet(
                first,
                _firstPick.PickPoint,
                second,
                secondPick.PickPoint,
                _radius,
                context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities,
                out _))
        {
            _previewEntities = Array.Empty<CadEntity>();
            return ToolResult.None();
        }

        _previewEntities = resultEntities ?? Array.Empty<CadEntity>();

        return _previewEntities.Count > 0
            ? ToolResult.Updated("Fillet preview updated.")
            : ToolResult.None();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.Cancelled("Fillet command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Reset(context);
        return ToolResult.None("Fillet tool deactivated.");
    }

    private ToolResult AcceptRadius(
        ToolContext context,
        double radius)
    {
        if (radius < 0)
        {
            return ToolResult.None("Fillet radius cannot be negative.");
        }

        _radius = radius;
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;

        return ToolResult.Started($"Fillet radius set to {_radius:0.###}. Select first line.");
    }

    private ToolResult AcceptFirstLine(
        ToolContext context,
        Point2D pickPoint)
    {
        ToolPickedEntityInput? pick = PickSelectableLine(context, pickPoint);

        if (pick is null)
        {
            return ToolResult.None("Select a visible, unlocked line as first fillet object.");
        }

        _firstPick = pick;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForSecondEntity;
        context.CurrentBasePoint = pick.ClosestPoint;

        return ToolResult.Started("Select second line.");
    }

    private ToolResult AcceptSecondLine(
        ToolContext context,
        Point2D pickPoint)
    {
        if (_firstPick is null)
        {
            State = FilletToolState.WaitingForFirstEntityOrRadius;
            return ToolResult.None("Select first line before selecting the second line.");
        }

        ToolPickedEntityInput? secondPick = PickSelectableLine(context, pickPoint);

        if (secondPick is null)
        {
            return ToolResult.None("Select a visible, unlocked line as second fillet object.");
        }

        if (secondPick.EntityId.Equals(_firstPick.EntityId))
        {
            return ToolResult.None("Second fillet object must be different from the first one.");
        }

        LineEntity first = (LineEntity)_firstPick.Entity;
        LineEntity second = (LineEntity)secondPick.Entity;

        if (!TryCreateLineLineFillet(
                first,
                _firstPick.PickPoint,
                second,
                secondPick.PickPoint,
                _radius,
                context.GeometryTolerance,
                out IReadOnlyList<CadEntity>? resultEntities,
                out string? errorMessage))
        {
            return ToolResult.None(errorMessage ?? "Cannot create fillet for the selected lines.");
        }

        if (resultEntities is null)
        {
            return ToolResult.None(errorMessage ?? "Cannot create fillet for the selected lines.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                new[] { first, second },
                resultEntities,
                _radius <= context.GeometryTolerance.Distance
                    ? "Fillet lines with zero radius"
                    : "Fillet lines"));

        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;

        return ToolResult.Completed("Fillet created. Select first line or type Radius.");
    }

    internal static bool TryCreateLineLineFillet(
        LineEntity first,
        Point2D firstPickPoint,
        LineEntity second,
        Point2D secondPickPoint,
        double radius,
        GeometryTolerance tolerance,
        out IReadOnlyList<CadEntity>? resultEntities,
        out string? errorMessage)
    {
        resultEntities = null;
        errorMessage = null;

        if (!LineIntersectionService.TryIntersectInfiniteLines(
                first.Geometry,
                second.Geometry,
                out LineIntersectionInfo intersection,
                tolerance))
        {
            errorMessage = "Cannot fillet parallel or coincident lines.";
            return false;
        }

        Vector2D firstDirection = first.Start.VectorTo(first.End);
        Vector2D secondDirection = second.Start.VectorTo(second.End);

        if (tolerance.IsVectorLengthZero(firstDirection.Length) ||
            tolerance.IsVectorLengthZero(secondDirection.Length))
        {
            errorMessage = "Cannot fillet zero-length lines.";
            return false;
        }

        Vector2D firstUnit = firstDirection.Normalize();
        Vector2D secondUnit = secondDirection.Normalize();
        Point2D intersectionPoint = intersection.Point;

        double firstPickParameter = GetLineParameter(first, firstPickPoint);
        double secondPickParameter = GetLineParameter(second, secondPickPoint);

        Vector2D firstBranch = firstPickParameter < intersection.FirstParameter
            ? firstUnit * -1.0
            : firstUnit;
        Vector2D secondBranch = secondPickParameter < intersection.SecondParameter
            ? secondUnit * -1.0
            : secondUnit;

        double branchDot = Math.Clamp(firstBranch.Dot(secondBranch), -1.0, 1.0);
        double angle = Math.Acos(branchDot);

        if (angle <= tolerance.Angle || Math.Abs(Math.PI - angle) <= tolerance.Angle)
        {
            errorMessage = "Cannot fillet lines with an invalid corner angle.";
            return false;
        }

        if (radius <= tolerance.Distance)
        {
            resultEntities = new CadEntity[]
            {
                CreateTrimmedLineToPoint(first, firstBranch, intersectionPoint),
                CreateTrimmedLineToPoint(second, secondBranch, intersectionPoint)
            };
            return true;
        }

        double tangentDistance = radius / Math.Tan(angle / 2.0);

        if (tangentDistance <= tolerance.Distance || double.IsNaN(tangentDistance) || double.IsInfinity(tangentDistance))
        {
            errorMessage = "Fillet radius is not valid for the selected angle.";
            return false;
        }

        Point2D firstTangent = intersectionPoint + firstBranch * tangentDistance;
        Point2D secondTangent = intersectionPoint + secondBranch * tangentDistance;
        Vector2D bisectorVector = firstBranch + secondBranch;

        if (tolerance.IsVectorLengthZero(bisectorVector.Length))
        {
            errorMessage = "Cannot fillet lines with a degenerate corner bisector.";
            return false;
        }

        Vector2D bisector = bisectorVector.Normalize();
        Point2D center = intersectionPoint + bisector * (radius / Math.Sin(angle / 2.0));

        Angle startAngle = Angle.FromRadians(
            Math.Atan2(
                firstTangent.Y - center.Y,
                firstTangent.X - center.X));
        Angle endAngle = Angle.FromRadians(
            Math.Atan2(
                secondTangent.Y - center.Y,
                secondTangent.X - center.X));
        bool isCounterClockwise = center.VectorTo(firstTangent).Cross(center.VectorTo(secondTangent)) > 0;

        var filletArc = new ArcEntity(
            center,
            radius,
            startAngle,
            endAngle,
            isCounterClockwise,
            layerId: first.LayerId,
            style: first.Style,
            isVisible: first.IsVisible,
            isLocked: first.IsLocked,
            drawOrder: Math.Max(first.DrawOrder, second.DrawOrder) + 1);

        resultEntities = new CadEntity[]
        {
            CreateTrimmedLineToPoint(first, firstBranch, firstTangent),
            CreateTrimmedLineToPoint(second, secondBranch, secondTangent),
            filletArc
        };
        return true;
    }

    private static LineEntity CreateTrimmedLineToPoint(
        LineEntity source,
        Vector2D keptBranch,
        Point2D endPoint)
    {
        Vector2D sourceDirection = source.Start.VectorTo(source.End).Normalize();
        bool keepStartSide = keptBranch.Dot(sourceDirection) < 0;

        return keepStartSide
            ? new LineEntity(
                source.Start,
                endPoint,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder)
            : new LineEntity(
                endPoint,
                source.End,
                layerId: source.LayerId,
                style: source.Style,
                isVisible: source.IsVisible,
                isLocked: source.IsLocked,
                drawOrder: source.DrawOrder);
    }

    private static double GetLineParameter(
        LineEntity line,
        Point2D point)
    {
        Vector2D direction = line.Start.VectorTo(line.End);
        double lengthSquared = direction.LengthSquared;

        if (lengthSquared <= 0)
        {
            return 0;
        }

        return line.Start.VectorTo(point).Dot(direction) / lengthSquared;
    }

    private static ToolPickedEntityInput? PickSelectableLine(
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

        if (entity is not LineEntity || !context.Document.IsEntitySelectable(entity))
        {
            return null;
        }

        return new ToolPickedEntityInput(
            selectedId.Value,
            pickPoint,
            entity.GetClosestPoint(pickPoint),
            entity);
    }

    private void Reset(ToolContext context)
    {
        _firstPick = null;
        _previewEntities = Array.Empty<CadEntity>();
        State = FilletToolState.WaitingForFirstEntityOrRadius;
        context.CurrentBasePoint = null;
    }
}
