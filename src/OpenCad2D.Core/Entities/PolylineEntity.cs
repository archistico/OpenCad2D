using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing an AutoCAD-style lightweight polyline.
/// Each segment can be straight or curved through a DXF-compatible bulge value.
/// A bulge of 0 means a straight segment; non-zero values describe circular arc segments.
/// </summary>
public sealed class PolylineEntity : CadEntity, IFillableEntity
{
    private const int DefaultArcApproximationSegments = 128;

    public PolylineEntity(
        IEnumerable<Point2D> vertices,
        bool isClosed = false,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0,
        bool isFilled = false,
        IEnumerable<double>? segmentBulges = null)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        Geometry = new Polyline2D(vertices, isClosed);
        IsFilled = isFilled;
        SegmentBulges = NormalizeBulges(segmentBulges, SegmentCount);
    }

    public Polyline2D Geometry { get; }

    public IReadOnlyList<Point2D> Vertices => Geometry.Vertices;

    public bool IsClosed => Geometry.IsClosed;

    public bool IsFilled { get; }

    public IReadOnlyList<double> SegmentBulges { get; }

    public int SegmentCount => IsClosed
        ? Vertices.Count
        : Math.Max(Vertices.Count - 1, 0);

    public bool HasArcSegments => SegmentBulges.Any(bulge => !Tolerance.IsZero(bulge));

    public override EntityKind Kind => EntityKind.Polyline;

    public override BoundingBox2D GetBoundingBox()
    {
        if (!HasArcSegments)
        {
            return Geometry.GetBoundingBox();
        }

        return ToPolylineApproximation().Geometry.GetBoundingBox();
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToPolyline(
            point,
            GetDistanceGeometry());
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnPolyline(
            point,
            GetDistanceGeometry());
    }

    public PolylineEntity ToPolylineApproximation(int arcApproximationSegments = DefaultArcApproximationSegments)
    {
        if (!HasArcSegments)
        {
            return this;
        }

        var points = new List<Point2D>();

        for (int index = 0; index < SegmentCount; index++)
        {
            Point2D start = Vertices[index];
            Point2D end = Vertices[(index + 1) % Vertices.Count];

            if (points.Count == 0)
            {
                points.Add(start);
            }

            double bulge = SegmentBulges[index];

            if (Tolerance.IsZero(bulge))
            {
                points.Add(end);
                continue;
            }

            foreach (Point2D point in ApproximateBulgeSegment(
                         start,
                         end,
                         bulge,
                         arcApproximationSegments).Skip(1))
            {
                points.Add(point);
            }
        }

        return new PolylineEntity(
            points,
            IsClosed,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled);
    }

    public Polyline2D GetInteractionGeometry(int arcApproximationSegments = DefaultArcApproximationSegments)
    {
        return HasArcSegments
            ? ToPolylineApproximation(arcApproximationSegments).Geometry
            : Geometry;
    }

    public IReadOnlyList<Point2D> GetSegmentMidpoints(int arcApproximationSegments = DefaultArcApproximationSegments)
    {
        var midpoints = new List<Point2D>();

        for (int index = 0; index < SegmentCount; index++)
        {
            Point2D start = Vertices[index];
            Point2D end = Vertices[(index + 1) % Vertices.Count];
            double bulge = SegmentBulges[index];

            if (Tolerance.IsZero(bulge))
            {
                midpoints.Add(new LineSegment2D(start, end).Midpoint);
                continue;
            }

            midpoints.Add(GetLengthMidpoint(ApproximateBulgeSegment(
                start,
                end,
                bulge,
                arcApproximationSegments)));
        }

        return midpoints;
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        var transformedVertices = Vertices
            .Select(matrix.Transform)
            .ToList();

        bool hasNegativeDeterminant = HasNegativeDeterminant(matrix);
        IReadOnlyList<double> transformedBulges = hasNegativeDeterminant
            ? SegmentBulges.Select(bulge => -bulge).ToList()
            : SegmentBulges;

        return new PolylineEntity(
            transformedVertices,
            IsClosed,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled,
            transformedBulges);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new PolylineEntity(
            Vertices,
            IsClosed,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled,
            SegmentBulges);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new PolylineEntity(
            Vertices,
            IsClosed,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled,
            SegmentBulges);
    }

    public CadEntity WithFill(bool isFilled)
    {
        return new PolylineEntity(
            Vertices,
            IsClosed,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            isFilled,
            SegmentBulges);
    }

    private Polyline2D GetDistanceGeometry()
    {
        return GetInteractionGeometry();
    }

    private static IReadOnlyList<double> NormalizeBulges(
        IEnumerable<double>? segmentBulges,
        int expectedCount)
    {
        if (expectedCount == 0)
        {
            return Array.Empty<double>();
        }

        if (segmentBulges is null)
        {
            return Enumerable.Repeat(0.0, expectedCount).ToList();
        }

        List<double> result = segmentBulges.ToList();

        if (result.Count != expectedCount)
        {
            throw new ArgumentException(
                $"Polyline bulge count must match segment count. Expected {expectedCount}, got {result.Count}.",
                nameof(segmentBulges));
        }

        return result;
    }


    private static Point2D GetLengthMidpoint(IReadOnlyList<Point2D> points)
    {
        if (points.Count == 0)
        {
            throw new ArgumentException("At least one point is required.", nameof(points));
        }

        if (points.Count == 1)
        {
            return points[0];
        }

        double totalLength = 0.0;

        for (int index = 0; index < points.Count - 1; index++)
        {
            totalLength += points[index].DistanceTo(points[index + 1]);
        }

        if (Tolerance.IsZero(totalLength))
        {
            return points[0];
        }

        double targetLength = totalLength / 2.0;
        double accumulatedLength = 0.0;

        for (int index = 0; index < points.Count - 1; index++)
        {
            Point2D start = points[index];
            Point2D end = points[index + 1];
            double segmentLength = start.DistanceTo(end);

            if (Tolerance.IsZero(segmentLength))
            {
                continue;
            }

            if (accumulatedLength + segmentLength >= targetLength)
            {
                double ratio = (targetLength - accumulatedLength) / segmentLength;
                return new Point2D(
                    start.X + ((end.X - start.X) * ratio),
                    start.Y + ((end.Y - start.Y) * ratio));
            }

            accumulatedLength += segmentLength;
        }

        return points[^1];
    }

    private static IReadOnlyList<Point2D> ApproximateBulgeSegment(
        Point2D start,
        Point2D end,
        double bulge,
        int requestedSegments)
    {
        if (Tolerance.ArePointsEqual(start, end) || Tolerance.IsZero(bulge))
        {
            return new[] { start, end };
        }

        double chordLength = start.DistanceTo(end);
        double sweep = -4.0 * Math.Atan(bulge);
        double includedAngle = Math.Abs(sweep);

        if (Tolerance.IsZero(includedAngle))
        {
            return new[] { start, end };
        }

        double radius = chordLength / (2.0 * Math.Sin(includedAngle / 2.0));
        Point2D midpoint = new(
            (start.X + end.X) / 2.0,
            (start.Y + end.Y) / 2.0);

        Vector2D chord = start.VectorTo(end).Normalize();
        Vector2D leftNormal = new(-chord.Y, chord.X);
        double centerOffset = chordLength * (1.0 - bulge * bulge) / (4.0 * bulge);
        Point2D center = midpoint - leftNormal * centerOffset;

        double startAngle = Math.Atan2(
            start.Y - center.Y,
            start.X - center.X);

        int segments = Math.Max(
            2,
            (int)Math.Ceiling(requestedSegments * includedAngle / (2.0 * Math.PI)));

        var points = new List<Point2D>(segments + 1)
        {
            start
        };

        for (int index = 1; index < segments; index++)
        {
            double t = index / (double)segments;
            double angle = startAngle + sweep * t;
            points.Add(new Point2D(
                center.X + Math.Cos(angle) * radius,
                center.Y + Math.Sin(angle) * radius));
        }

        points.Add(end);

        return points;
    }

    private static bool HasNegativeDeterminant(Matrix2D matrix)
    {
        double determinant = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21);

        return determinant < 0;
    }
}
