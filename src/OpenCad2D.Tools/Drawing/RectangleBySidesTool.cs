using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw a rectangular closed polyline from a start point,
/// a first side endpoint and a point defining the opposite side distance.
/// </summary>
public sealed class RectangleBySidesTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider
{
    private Point2D? _startPoint;
    private Point2D? _firstSideEndPoint;
    private Point2D? _currentPoint;

    public string Name => "Rectangle Sides";

    public RectangleBySidesToolState State { get; private set; } =
        RectangleBySidesToolState.WaitingForStartPoint;

    public Point2D? StartPoint => _startPoint;

    public Point2D? FirstSideEndPoint => _firstSideEndPoint;

    public Point2D? CurrentPoint => _currentPoint;


    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            RectangleBySidesToolState.WaitingForStartPoint => new CommandPromptState(
                "RECTSIDES",
                "Specify first corner",
                CommandInputKind.Point,
                placeholder: "100,50"),

            RectangleBySidesToolState.WaitingForFirstSideEndPoint => new CommandPromptState(
                "RECTSIDES",
                "Specify first side endpoint",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0   |   @100<45"),

            RectangleBySidesToolState.WaitingForSecondSidePoint => new CommandPromptState(
                "RECTSIDES",
                "Specify second side point or type exact length",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   distance"),

            _ => new CommandPromptState(
                "RECTSIDES",
                "Specify first corner",
                CommandInputKind.Point,
                placeholder: "100,50")
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == RectangleBySidesToolState.WaitingForSecondSidePoint &&
            IsExactSecondSideLengthInput(input))
        {
            return SelectSecondSideLength(context, input.Distance!.Value);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "RECTSIDES expects a point or distance input.");
        }

        return SubmitResolvedPoint(context, input.Point.Value);
    }

    private static bool IsExactSecondSideLengthInput(CommandInputSubmission input)
    {
        if (input.Distance is null)
        {
            return false;
        }

        if (input.Kind == CommandInputSubmissionKind.Distance)
        {
            return true;
        }

        if (input.Kind != CommandInputSubmissionKind.Point)
        {
            return false;
        }

        string rawText = input.RawText.Trim();

        return rawText.Length > 0 &&
            !rawText.StartsWith('@') &&
            !rawText.Contains(',') &&
            !rawText.Contains('<');
    }

    public bool HasPreview =>
        GetFirstSidePreviewEntity() is not null ||
        GetPreviewEntity() is not null;

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return State switch
        {
            RectangleBySidesToolState.WaitingForStartPoint => SelectStartPoint(
                context,
                pointer),

            RectangleBySidesToolState.WaitingForFirstSideEndPoint => SelectFirstSideEndPoint(
                context,
                pointer),

            RectangleBySidesToolState.WaitingForSecondSidePoint => SelectSecondSidePoint(
                context,
                pointer),

            _ => ToolResult.None()
        };
    }


    private ToolResult SubmitResolvedPoint(
        ToolContext context,
        Point2D point)
    {
        return State switch
        {
            RectangleBySidesToolState.WaitingForStartPoint => SelectStartPoint(context, point),
            RectangleBySidesToolState.WaitingForFirstSideEndPoint => SelectFirstSideEndPoint(context, point),
            RectangleBySidesToolState.WaitingForSecondSidePoint => SelectSecondSidePoint(context, point),
            _ => ToolResult.None()
        };
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State == RectangleBySidesToolState.WaitingForStartPoint)
        {
            return ToolResult.None();
        }

        Point2D? basePoint = State == RectangleBySidesToolState.WaitingForFirstSideEndPoint
            ? _startPoint
            : _startPoint;

        if (basePoint is null)
        {
            return ToolResult.None();
        }

        _currentPoint = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            basePoint,
            applyAngleConstraint: State == RectangleBySidesToolState.WaitingForFirstSideEndPoint);

        return ToolResult.Updated();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Rectangle Sides command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Rectangle Sides tool deactivated.");
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previews = new List<CadEntity>();

        LineEntity? firstSidePreview = GetFirstSidePreviewEntity();
        if (firstSidePreview is not null)
        {
            previews.Add(firstSidePreview);
        }

        PolylineEntity? rectanglePreview = GetPreviewEntity();
        if (rectanglePreview is not null)
        {
            previews.Add(rectanglePreview);
        }

        return previews;
    }

    public LineEntity? GetFirstSidePreviewEntity()
    {
        if (State != RectangleBySidesToolState.WaitingForFirstSideEndPoint ||
            _startPoint is null ||
            _currentPoint is null)
        {
            return null;
        }

        if (OpenCad2D.Geometry.Tolerance.IsZero(
                _startPoint.Value.DistanceTo(_currentPoint.Value)))
        {
            return null;
        }

        return new LineEntity(
            _startPoint.Value,
            _currentPoint.Value);
    }

    public PolylineEntity? GetPreviewEntity()
    {
        if (_startPoint is null ||
            _firstSideEndPoint is null ||
            _currentPoint is null)
        {
            return null;
        }

        return TryCreateRectangleEntity(
            _startPoint.Value,
            _firstSideEndPoint.Value,
            _currentPoint.Value,
            layerId: null,
            out PolylineEntity? rectangle)
            ? rectangle
            : null;
    }

    private ToolResult SelectStartPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            basePoint: null,
            applyAngleConstraint: false);

        return SelectStartPoint(context, point);
    }

    private ToolResult SelectStartPoint(
        ToolContext context,
        Point2D point)
    {
        _startPoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = point;
        State = RectangleBySidesToolState.WaitingForFirstSideEndPoint;

        return ToolResult.Started("Specify first side endpoint.");
    }

    private ToolResult SelectFirstSideEndPoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_startPoint is null)
        {
            throw new InvalidOperationException(
                "Rectangle Sides tool is waiting for first side endpoint but start point is missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _startPoint,
            applyAngleConstraint: true);

        return SelectFirstSideEndPoint(context, point);
    }

    private ToolResult SelectFirstSideEndPoint(
        ToolContext context,
        Point2D point)
    {
        if (_startPoint is null)
        {
            throw new InvalidOperationException(
                "Rectangle Sides tool is waiting for first side endpoint but start point is missing.");
        }

        if (context.GeometryTolerance.ArePointsEqual(
                _startPoint.Value,
                point))
        {
            return ToolResult.None("First side length must be greater than zero.");
        }

        _firstSideEndPoint = point;
        _currentPoint = point;
        context.CurrentBasePoint = _startPoint;
        State = RectangleBySidesToolState.WaitingForSecondSidePoint;

        return ToolResult.Started("Specify second side point.");
    }

    private ToolResult SelectSecondSidePoint(
        ToolContext context,
        PointerInfo pointer)
    {
        if (_startPoint is null || _firstSideEndPoint is null)
        {
            throw new InvalidOperationException(
                "Rectangle Sides tool is waiting for second side point but previous points are missing.");
        }

        Point2D point = ResolveInputPoint(
            context,
            pointer.ModelPoint,
            _startPoint,
            applyAngleConstraint: false);

        return SelectSecondSidePoint(context, point);
    }

    private ToolResult SelectSecondSidePoint(
        ToolContext context,
        Point2D point)
    {
        if (_startPoint is null || _firstSideEndPoint is null)
        {
            throw new InvalidOperationException(
                "Rectangle Sides tool is waiting for second side point but previous points are missing.");
        }

        if (!TryCreateRectangleEntity(
                _startPoint.Value,
                _firstSideEndPoint.Value,
                point,
                context.Creation.CurrentLayerId,
                out PolylineEntity? rectangle))
        {
            return ToolResult.None("Second side length must be greater than zero.");
        }

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(rectangle));

        Reset(context);

        return ToolResult.Completed("Rectangle Sides created.");
    }


    private ToolResult SelectSecondSideLength(
        ToolContext context,
        double length)
    {
        if (_startPoint is null || _firstSideEndPoint is null)
        {
            throw new InvalidOperationException(
                "Rectangle Sides tool is waiting for second side length but previous points are missing.");
        }

        if (length <= context.GeometryTolerance.Distance)
        {
            return ToolResult.None("Second side length must be greater than zero.");
        }

        double signedHeight = DetermineSecondSideSign(
            _startPoint.Value,
            _firstSideEndPoint.Value,
            _currentPoint) * length;

        if (!TryCreateRectangleEntityFromSignedHeight(
                _startPoint.Value,
                _firstSideEndPoint.Value,
                signedHeight,
                context.Creation.CurrentLayerId,
                out PolylineEntity? rectangle))
        {
            return ToolResult.None("Second side length must be greater than zero.");
        }

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(rectangle));

        Reset(context);

        return ToolResult.Completed("Rectangle Sides created.");
    }

    private static bool TryCreateRectangleEntity(
        Point2D startPoint,
        Point2D firstSideEndPoint,
        Point2D secondSidePoint,
        OpenCad2D.Core.Identifiers.LayerId? layerId,
        out PolylineEntity rectangle)
    {
        Vector2D firstSide = startPoint.VectorTo(firstSideEndPoint);
        double firstSideLength = firstSide.Length;

        if (OpenCad2D.Geometry.Tolerance.IsZero(firstSideLength))
        {
            rectangle = null!;
            return false;
        }

        Vector2D firstSideDirection = firstSide / firstSideLength;
        Vector2D perpendicularDirection = firstSideDirection.PerpendicularLeft();
        Vector2D candidate = startPoint.VectorTo(secondSidePoint);
        double signedHeight = candidate.Dot(perpendicularDirection);

        if (OpenCad2D.Geometry.Tolerance.IsZero(signedHeight))
        {
            rectangle = null!;
            return false;
        }

        Vector2D secondSide = perpendicularDirection * signedHeight;

        var vertices = new[]
        {
            startPoint,
            firstSideEndPoint,
            firstSideEndPoint + secondSide,
            startPoint + secondSide
        };

        rectangle = new PolylineEntity(
            vertices,
            isClosed: true,
            layerId: layerId);

        return true;
    }


    private static bool TryCreateRectangleEntityFromSignedHeight(
        Point2D startPoint,
        Point2D firstSideEndPoint,
        double signedHeight,
        OpenCad2D.Core.Identifiers.LayerId? layerId,
        out PolylineEntity rectangle)
    {
        Vector2D firstSide = startPoint.VectorTo(firstSideEndPoint);
        double firstSideLength = firstSide.Length;

        if (OpenCad2D.Geometry.Tolerance.IsZero(firstSideLength) ||
            OpenCad2D.Geometry.Tolerance.IsZero(signedHeight))
        {
            rectangle = null!;
            return false;
        }

        Vector2D firstSideDirection = firstSide / firstSideLength;
        Vector2D secondSide = firstSideDirection.PerpendicularLeft() * signedHeight;

        var vertices = new[]
        {
            startPoint,
            firstSideEndPoint,
            firstSideEndPoint + secondSide,
            startPoint + secondSide
        };

        rectangle = new PolylineEntity(
            vertices,
            isClosed: true,
            layerId: layerId);

        return true;
    }

    private static double DetermineSecondSideSign(
        Point2D startPoint,
        Point2D firstSideEndPoint,
        Point2D? currentPoint)
    {
        if (currentPoint is null)
        {
            return 1.0;
        }

        Vector2D firstSide = startPoint.VectorTo(firstSideEndPoint);
        double firstSideLength = firstSide.Length;

        if (OpenCad2D.Geometry.Tolerance.IsZero(firstSideLength))
        {
            return 1.0;
        }

        Vector2D perpendicularDirection = (firstSide / firstSideLength).PerpendicularLeft();
        double signedDistance = startPoint.VectorTo(currentPoint.Value).Dot(perpendicularDirection);

        return signedDistance < 0 ? -1.0 : 1.0;
    }

    private static Point2D ResolveInputPoint(
        ToolContext context,
        Point2D cursorPoint,
        Point2D? basePoint,
        bool applyAngleConstraint)
    {
        Point2D point = ApplySnap(
            context,
            cursorPoint,
            basePoint);

        if (applyAngleConstraint && basePoint is not null)
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
            context.GeometryTolerance.IsDistanceZero(context.SnapTolerance))
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

    private void Reset(ToolContext? context = null)
    {
        _startPoint = null;
        _firstSideEndPoint = null;
        _currentPoint = null;
        State = RectangleBySidesToolState.WaitingForStartPoint;

        if (context is not null)
        {
            context.CurrentBasePoint = null;
        }
    }
}
