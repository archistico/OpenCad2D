using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Calculates AutoCAD-style DIVIDE points for supported curve entities.
/// DIVIDE does not split the source entity; it only returns persistent point positions.
/// </summary>
public sealed class DivideEntityService
{
    public const int MinimumSegmentCount = 2;
    public const int MaximumSegmentCount = 1000;

    public DivideEntityResult Divide(
        CadEntity entity,
        int segmentCount)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (segmentCount < MinimumSegmentCount || segmentCount > MaximumSegmentCount)
        {
            return DivideEntityResult.Failure(
                $"Segment count must be an integer between {MinimumSegmentCount} and {MaximumSegmentCount}.");
        }

        return entity switch
        {
            LineEntity line => DivideLine(line, segmentCount),
            ArcEntity arc => DivideArc(arc, segmentCount),
            CircleEntity circle => DivideCircle(circle, segmentCount),
            PolylineEntity polyline => DividePolyline(polyline, segmentCount),
            _ => DivideEntityResult.Failure("Selected entity cannot be divided.")
        };
    }

    public bool CanDivide(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity is LineEntity or ArcEntity or CircleEntity or PolylineEntity;
    }

    private static DivideEntityResult DivideLine(
        LineEntity line,
        int segmentCount)
    {
        double length = line.Start.DistanceTo(line.End);
        if (Tolerance.IsZero(length))
        {
            return DivideEntityResult.Failure("Cannot divide a zero-length entity.");
        }

        var points = new List<Point2D>(segmentCount - 1);
        Vector2D delta = line.Start.VectorTo(line.End);

        for (int index = 1; index < segmentCount; index++)
        {
            double ratio = index / (double)segmentCount;
            points.Add(line.Start + (delta * ratio));
        }

        return DivideEntityResult.Success(points);
    }

    private static DivideEntityResult DivideArc(
        ArcEntity arc,
        int segmentCount)
    {
        double sweep = GetSignedArcSweepRadians(arc);
        if (Tolerance.IsZero(sweep))
        {
            return DivideEntityResult.Failure("Cannot divide a zero-length entity.");
        }

        var points = new List<Point2D>(segmentCount - 1);

        for (int index = 1; index < segmentCount; index++)
        {
            double ratio = index / (double)segmentCount;
            double angle = arc.StartAngle.Radians + (sweep * ratio);
            points.Add(arc.Geometry.PointAt(Angle.FromRadians(angle)));
        }

        return DivideEntityResult.Success(points);
    }

    private static DivideEntityResult DivideCircle(
        CircleEntity circle,
        int segmentCount)
    {
        var points = new List<Point2D>(segmentCount);

        for (int index = 0; index < segmentCount; index++)
        {
            double angle = 2.0 * Math.PI * index / segmentCount;
            points.Add(circle.Geometry.PointAt(Angle.FromRadians(angle)));
        }

        return DivideEntityResult.Success(points);
    }

    private static DivideEntityResult DividePolyline(
        PolylineEntity polyline,
        int segmentCount)
    {
        Polyline2D geometry = polyline.GetInteractionGeometry();
        IReadOnlyList<LineSegment2D> segments = geometry.GetSegments();
        double totalLength = segments.Sum(segment => segment.Length);

        if (Tolerance.IsZero(totalLength))
        {
            return DivideEntityResult.Failure("Cannot divide a zero-length entity.");
        }

        int pointCount = polyline.IsClosed
            ? segmentCount
            : segmentCount - 1;

        var points = new List<Point2D>(pointCount);
        double step = totalLength / segmentCount;

        for (int index = polyline.IsClosed ? 0 : 1;
             index < (polyline.IsClosed ? segmentCount : segmentCount);
             index++)
        {
            double targetDistance = step * index;
            points.Add(GetPointAtDistance(segments, targetDistance, totalLength));
        }

        return DivideEntityResult.Success(points);
    }

    private static Point2D GetPointAtDistance(
        IReadOnlyList<LineSegment2D> segments,
        double targetDistance,
        double totalLength)
    {
        if (targetDistance <= 0)
        {
            return segments[0].Start;
        }

        if (targetDistance >= totalLength)
        {
            return segments[^1].End;
        }

        double accumulated = 0.0;

        foreach (LineSegment2D segment in segments)
        {
            double segmentLength = segment.Length;
            if (Tolerance.IsZero(segmentLength))
            {
                continue;
            }

            if (accumulated + segmentLength >= targetDistance)
            {
                double ratio = (targetDistance - accumulated) / segmentLength;
                return segment.Start + (segment.Start.VectorTo(segment.End) * ratio);
            }

            accumulated += segmentLength;
        }

        return segments[^1].End;
    }

    private static double GetSignedArcSweepRadians(ArcEntity arc)
    {
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;

        if (arc.IsCounterClockwise)
        {
            if (end <= start)
            {
                end += 2.0 * Math.PI;
            }

            return end - start;
        }

        if (end >= start)
        {
            end -= 2.0 * Math.PI;
        }

        return end - start;
    }
}
