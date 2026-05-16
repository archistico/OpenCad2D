using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing an open or closed Bezier spline defined by control points.
/// The curve is rendered and edited natively, while modify operations may use a sampled polyline approximation.
/// </summary>
public sealed class BezierSplineEntity : CadEntity
{
    public const int DefaultSampleCount = 96;

    private readonly IReadOnlyList<Point2D> _controlPoints;

    public BezierSplineEntity(
        IEnumerable<Point2D> controlPoints,
        bool isClosed = false,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        ArgumentNullException.ThrowIfNull(controlPoints);

        var points = controlPoints.ToList();
        if (points.Count < 2)
        {
            throw new ArgumentException(
                "A spline requires at least two control points.",
                nameof(controlPoints));
        }

        _controlPoints = points;
        IsClosed = isClosed;
    }

    public IReadOnlyList<Point2D> ControlPoints => _controlPoints;

    public bool IsClosed { get; }

    public override EntityKind Kind => EntityKind.BezierSpline;

    public override BoundingBox2D GetBoundingBox()
    {
        return ToPolylineApproximation().GetBoundingBox();
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToPolyline(
            point,
            ToPolylineApproximation().Geometry);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnPolyline(
            point,
            ToPolylineApproximation().Geometry);
    }

    public IReadOnlyList<Point2D> GetSamplePoints(int sampleCount = DefaultSampleCount)
    {
        if (sampleCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleCount),
                "Spline sample count must be at least two.");
        }

        IReadOnlyList<Point2D> effectiveControlPoints = GetEffectiveControlPoints();
        int count = IsClosed ? sampleCount : sampleCount + 1;
        var points = new List<Point2D>(count);

        for (int i = 0; i < count; i++)
        {
            double t = IsClosed
                ? (double)i / sampleCount
                : (double)i / sampleCount;

            if (!IsClosed && i == count - 1)
            {
                t = 1.0;
            }

            points.Add(EvaluateBezier(effectiveControlPoints, t));
        }

        return points;
    }

    public PolylineEntity ToPolylineApproximation(int sampleCount = DefaultSampleCount)
    {
        IReadOnlyList<Point2D> samples = GetSamplePoints(sampleCount);
        return new PolylineEntity(
            samples,
            IsClosed,
            layerId: LayerId,
            style: Style,
            isVisible: IsVisible,
            isLocked: IsLocked,
            drawOrder: DrawOrder);
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new BezierSplineEntity(
            ControlPoints.Select(matrix.Transform),
            IsClosed,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new BezierSplineEntity(
            ControlPoints,
            IsClosed,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new BezierSplineEntity(
            ControlPoints,
            IsClosed,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public BezierSplineEntity WithControlPoints(IEnumerable<Point2D> controlPoints)
    {
        return new BezierSplineEntity(
            controlPoints,
            IsClosed,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    private IReadOnlyList<Point2D> GetEffectiveControlPoints()
    {
        if (!IsClosed)
        {
            return ControlPoints;
        }

        var points = ControlPoints.ToList();
        if (!points[0].Equals(points[^1]))
        {
            points.Add(points[0]);
        }

        return points;
    }

    private static Point2D EvaluateBezier(
        IReadOnlyList<Point2D> controlPoints,
        double t)
    {
        if (controlPoints.Count == 1)
        {
            return controlPoints[0];
        }

        var working = controlPoints.ToList();
        t = Math.Clamp(t, 0.0, 1.0);

        for (int level = working.Count - 1; level > 0; level--)
        {
            for (int i = 0; i < level; i++)
            {
                working[i] = Lerp(working[i], working[i + 1], t);
            }
        }

        return working[0];
    }

    private static Point2D Lerp(Point2D first, Point2D second, double t)
    {
        return new Point2D(
            first.X + ((second.X - first.X) * t),
            first.Y + ((second.Y - first.Y) * t));
    }
}
