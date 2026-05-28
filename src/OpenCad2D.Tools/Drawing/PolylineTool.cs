using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw open or closed AutoCAD-style polylines.
/// Straight and curved segments are stored in the same polyline through per-segment bulges.
/// </summary>
public sealed class PolylineTool : ICadTool, ICommandDrivenTool, IKeyboardAwareTool, IToolPreviewEntityProvider
{
    private readonly List<Point2D> _vertices = new();
    private readonly List<double> _segmentBulges = new();
    private Point2D? _currentPoint;
    private Point2D? _arcPointOnArc;

    public string Name => "Polyline";

    public PolylineToolState State { get; private set; } =
        PolylineToolState.WaitingForFirstPoint;

    public IReadOnlyList<Point2D> Vertices => _vertices;

    public IReadOnlyList<double> SegmentBulges => _segmentBulges;

    public Point2D? CurrentPoint => _currentPoint;

    public Point2D? ArcPointOnArc => _arcPointOnArc;

    /// <summary>
    /// User-facing segment mode shown by UI and tests.
    /// </summary>
    public string SegmentMode => State is PolylineToolState.WaitingForArcPointOnArc or PolylineToolState.WaitingForArcEndPoint
        ? "Arc 3P"
        : "Line";

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State == PolylineToolState.WaitingForFirstPoint)
        {
            return new CommandPromptState(
                "POLYLINE",
                "Specify first point",
                CommandInputKind.Point,
                placeholder: "100,50");
        }

        if (State == PolylineToolState.WaitingForArcPointOnArc)
        {
            return new CommandPromptState(
                "POLYLINE ARC",
                "Specify point on arc",
                CommandInputKind.PointOrDistanceOrOption,
                new[]
                {
                    new CommandOption("Line", "L", "Return to straight segments"),
                    new CommandOption("Undo", "U", "Remove the last polyline vertex")
                },
                placeholder: "100,50   |   @50,25   |   L   |   U");
        }

        if (State == PolylineToolState.WaitingForArcEndPoint)
        {
            return new CommandPromptState(
                "POLYLINE ARC",
                "Specify arc endpoint",
                CommandInputKind.PointOrDistanceOrOption,
                new[]
                {
                    new CommandOption("Line", "L", "Cancel the pending arc and return to straight segments"),
                    new CommandOption("Undo", "U", "Cancel the pending arc point")
                },
                placeholder: "100,50   |   @50,0   |   L   |   U");
        }

        return new CommandPromptState(
            "POLYLINE LINE",
            "Specify next point",
            CommandInputKind.PointOrDistanceOrOption,
            new[]
            {
                new CommandOption("Arc", "A", "Draw the next segment as a three-point arc"),
                new CommandOption("Close", "C", "Close the polyline"),
                new CommandOption("Undo", "U", "Remove the last polyline vertex")
            },
            acceptsEmptyEnter: true,
            placeholder: "100,50   |   @50,0   |   @100<45   |   distance   |   A   |   C   |   U");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return State == PolylineToolState.CollectingVertices
                ? CompleteOpen(context)
                : ToolResult.None("Complete the current arc segment before finishing the polyline.");
        }

        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            return HandleOption(input.OptionKeyword, context);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "POLYLINE expects a point input.");
        }

        return SubmitResolvedPoint(context, input.Point.Value);
    }

    public bool HasPreview =>
        _vertices.Count > 0 &&
        _currentPoint.HasValue &&
        State != PolylineToolState.WaitingForFirstPoint;

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
        if (!HasPreview)
        {
            return null;
        }

        List<Point2D> previewVertices = _vertices.ToList();
        List<double> previewBulges = _segmentBulges.ToList();

        if (_currentPoint is not null &&
            !AreSamePoint(previewVertices[^1], _currentPoint.Value))
        {
            previewVertices.Add(_currentPoint.Value);
            previewBulges.Add(GetPreviewBulge(previewVertices[^2], _currentPoint.Value));
        }

        if (previewVertices.Count < 2)
        {
            return null;
        }

        return new PolylineEntity(
            previewVertices,
            isClosed: false,
            segmentBulges: previewBulges);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D point = ResolvePoint(context, pointer.ModelPoint);

        return SubmitResolvedPoint(context, point);
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (State == PolylineToolState.WaitingForFirstPoint ||
            _vertices.Count == 0)
        {
            return ToolResult.None();
        }

        _currentPoint = ResolvePoint(context, pointer.ModelPoint);

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

    public bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (key == CadToolKey.U &&
            State != PolylineToolState.WaitingForFirstPoint)
        {
            result = UndoLastVertex(context);
            return true;
        }

        if (key == CadToolKey.L &&
            State is PolylineToolState.WaitingForArcPointOnArc or PolylineToolState.WaitingForArcEndPoint)
        {
            result = ReturnToLineMode(context);
            return true;
        }

        if (State == PolylineToolState.CollectingVertices &&
            key == CadToolKey.A)
        {
            result = StartArcSegment(context);
            return true;
        }

        if (State == PolylineToolState.CollectingVertices &&
            key == CadToolKey.Enter)
        {
            result = CompleteOpen(context);
            return true;
        }

        if (State == PolylineToolState.CollectingVertices &&
            key == CadToolKey.C)
        {
            result = CompleteClosed(context);
            return true;
        }

        result = ToolResult.None();
        return false;
    }

    public ToolResult CompleteOpen(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State != PolylineToolState.CollectingVertices)
        {
            return ToolResult.None("Complete the current arc segment before finishing the polyline.");
        }

        if (_vertices.Count < 2)
        {
            return ToolResult.None("Polyline requires at least two points.");
        }

        return Commit(
            context,
            isClosed: false,
            message: "Polyline created.");
    }

    public ToolResult CompleteClosed(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State != PolylineToolState.CollectingVertices)
        {
            return ToolResult.None("Complete the current arc segment before closing the polyline.");
        }

        if (_vertices.Count < 3)
        {
            return ToolResult.None("Closed polyline requires at least three points.");
        }

        return Commit(
            context,
            isClosed: true,
            message: "Closed polyline created.");
    }

    public ToolResult UndoLastVertex(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_vertices.Count == 0)
        {
            return ToolResult.None("Nothing to undo.");
        }

        if (State == PolylineToolState.WaitingForArcEndPoint)
        {
            _arcPointOnArc = null;
            _currentPoint = _vertices[^1];
            State = PolylineToolState.WaitingForArcPointOnArc;
            context.CurrentBasePoint = _vertices[^1];

            return ToolResult.Updated("Polyline arc mode: specify point on arc, or L to return to line mode.");
        }

        if (State == PolylineToolState.WaitingForArcPointOnArc)
        {
            State = PolylineToolState.CollectingVertices;
            _arcPointOnArc = null;
            _currentPoint = _vertices[^1];
            context.CurrentBasePoint = _vertices[^1];

            return ToolResult.Updated("Polyline line mode: specify next point, or A for arc.");
        }

        if (State != PolylineToolState.CollectingVertices)
        {
            return ToolResult.None("Nothing to undo.");
        }

        _vertices.RemoveAt(_vertices.Count - 1);

        if (_segmentBulges.Count > 0)
        {
            _segmentBulges.RemoveAt(_segmentBulges.Count - 1);
        }

        if (_vertices.Count == 0)
        {
            Reset(context);
            return ToolResult.Updated("Specify first polyline point.");
        }

        _currentPoint = _vertices[^1];
        context.CurrentBasePoint = _vertices[^1];

        return ToolResult.Updated("Polyline line mode: specify next point, press Enter/right-click to finish, C to close, or A for arc.");
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.Cancelled("Polyline command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset(context);

        return ToolResult.None("Polyline tool deactivated.");
    }

    private ToolResult HandleOption(
        string? optionKeyword,
        ToolContext context)
    {
        return optionKeyword?.ToUpperInvariant() switch
        {
            "A" or "ARC" => StartArcSegment(context),
            "C" or "CLOSE" => CompleteClosed(context),
            "L" or "LINE" => ReturnToLineMode(context),
            "U" or "UNDO" => UndoLastVertex(context),
            _ => ToolResult.None("Unknown polyline option.")
        };
    }

    private ToolResult StartArcSegment(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State != PolylineToolState.CollectingVertices || _vertices.Count == 0)
        {
            return ToolResult.None("Specify the first polyline point before drawing an arc segment.");
        }

        State = PolylineToolState.WaitingForArcPointOnArc;
        _arcPointOnArc = null;
        _currentPoint = _vertices[^1];
        context.CurrentBasePoint = _vertices[^1];

        return ToolResult.Updated("Polyline arc mode: specify point on arc, or L to return to line mode.");
    }

    private ToolResult ReturnToLineMode(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_vertices.Count == 0)
        {
            return ToolResult.None("Specify the first polyline point first.");
        }

        State = PolylineToolState.CollectingVertices;
        _arcPointOnArc = null;
        _currentPoint = _vertices[^1];
        context.CurrentBasePoint = _vertices[^1];

        return ToolResult.Updated("Polyline line mode: specify next point.");
    }

    private ToolResult SubmitResolvedPoint(
        ToolContext context,
        Point2D point)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State == PolylineToolState.WaitingForFirstPoint)
        {
            _vertices.Add(point);
            _currentPoint = point;
            State = PolylineToolState.CollectingVertices;
            context.CurrentBasePoint = point;

            return ToolResult.Started("Polyline line mode: specify next point, press Enter/right-click to finish, C to close, or A for arc.");
        }

        if (State == PolylineToolState.WaitingForArcPointOnArc)
        {
            if (AreSamePoint(_vertices[^1], point, context.GeometryTolerance))
            {
                return ToolResult.None("Arc point must be different from the previous polyline point.");
            }

            _arcPointOnArc = point;
            _currentPoint = point;
            State = PolylineToolState.WaitingForArcEndPoint;
            context.CurrentBasePoint = _vertices[^1];

            return ToolResult.Updated("Polyline arc mode: specify arc endpoint, or L to return to line mode.");
        }

        if (State == PolylineToolState.WaitingForArcEndPoint)
        {
            if (_arcPointOnArc is null)
            {
                State = PolylineToolState.WaitingForArcPointOnArc;
                return ToolResult.None("Specify point on arc first.");
            }

            return SubmitArcEndPoint(context, point, _arcPointOnArc.Value);
        }

        if (State == PolylineToolState.CollectingVertices)
        {
            if (_vertices.Count > 0 &&
                AreSamePoint(_vertices[^1], point, context.GeometryTolerance))
            {
                return ToolResult.None("Polyline point must be different from previous point.");
            }

            _vertices.Add(point);
            _segmentBulges.Add(0.0);
            _currentPoint = point;
            context.CurrentBasePoint = point;

            return ToolResult.Updated("Polyline line mode: specify next point, press Enter/right-click to finish, C to close, or A for arc.");
        }

        return ToolResult.None();
    }

    private ToolResult SubmitArcEndPoint(
        ToolContext context,
        Point2D endPoint,
        Point2D pointOnArc)
    {
        Point2D startPoint = _vertices[^1];

        if (AreSamePoint(startPoint, endPoint, context.GeometryTolerance) ||
            AreSamePoint(pointOnArc, endPoint, context.GeometryTolerance))
        {
            return ToolResult.None("Arc endpoint must be different from the previous arc points.");
        }

        if (!ArcCreationService.TryCreateFromThreePoints(
                startPoint,
                pointOnArc,
                endPoint,
                context.GeometryTolerance,
                out Arc2D arc))
        {
            return ToolResult.None("The three arc points must not be collinear.");
        }

        _vertices.Add(endPoint);
        _segmentBulges.Add(GetBulgeFromArc(arc));
        _currentPoint = endPoint;
        _arcPointOnArc = null;
        State = PolylineToolState.CollectingVertices;
        context.CurrentBasePoint = endPoint;

        return ToolResult.Updated("Arc segment added. Polyline line mode: specify next point, or A for another arc.");
    }

    private ToolResult Commit(
        ToolContext context,
        bool isClosed,
        string message)
    {
        List<double> bulges = _segmentBulges.ToList();

        if (isClosed)
        {
            bulges.Add(0.0);
        }

        var polyline = new PolylineEntity(
            _vertices.ToList(),
            isClosed,
            layerId: context.Creation.CurrentLayerId,
            segmentBulges: bulges);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(polyline));

        Reset(context);

        return ToolResult.Completed(message);
    }

    private Point2D ResolvePoint(
        ToolContext context,
        Point2D cursorPoint)
    {
        Point2D? basePoint = _vertices.Count > 0
            ? _vertices[^1]
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

    private double GetPreviewBulge(Point2D startPoint, Point2D endPoint)
    {
        if (State != PolylineToolState.WaitingForArcEndPoint ||
            _arcPointOnArc is null ||
            AreSamePoint(startPoint, endPoint))
        {
            return 0.0;
        }

        return ArcCreationService.TryCreateFromThreePoints(
            startPoint,
            _arcPointOnArc.Value,
            endPoint,
            out Arc2D arc)
            ? GetBulgeFromArc(arc)
            : 0.0;
    }

    private static double GetBulgeFromArc(Arc2D arc)
    {
        double sweep = GetPositiveSweep(arc);
        double bulge = Math.Tan(sweep / 4.0);

        return arc.IsCounterClockwise
            ? -bulge
            : bulge;
    }

    private static double GetPositiveSweep(Arc2D arc)
    {
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;

        if (arc.IsCounterClockwise)
        {
            double sweep = end - start;
            return sweep < 0.0
                ? sweep + (2.0 * Math.PI)
                : sweep;
        }

        double clockwiseSweep = start - end;
        return clockwiseSweep < 0.0
            ? clockwiseSweep + (2.0 * Math.PI)
            : clockwiseSweep;
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
        _vertices.Clear();
        _segmentBulges.Clear();
        _currentPoint = null;
        _arcPointOnArc = null;
        State = PolylineToolState.WaitingForFirstPoint;
        context.CurrentBasePoint = null;
    }

    private static bool AreSamePoint(Point2D first, Point2D second)
    {
        return GeometryTolerance.Default.ArePointsEqual(first, second);
    }

    private static bool AreSamePoint(
        Point2D first,
        Point2D second,
        GeometryTolerance tolerance)
    {
        return tolerance.ArePointsEqual(first, second);
    }
}
