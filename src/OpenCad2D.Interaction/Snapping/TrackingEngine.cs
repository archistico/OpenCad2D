using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Builds temporary tracking lines from smart points and resolves cursor points near those lines.
/// </summary>
public sealed class TrackingEngine
{
    private static readonly Vector2D HorizontalDirection = new(1, 0);
    private static readonly Vector2D VerticalDirection = new(0, 1);
    private const double ParallelTolerance = 1e-9;

    public IReadOnlyList<TrackingLine> BuildAxisLines(IEnumerable<SmartPoint> smartPoints)
    {
        ArgumentNullException.ThrowIfNull(smartPoints);

        var lines = new List<TrackingLine>();

        foreach (SmartPoint smartPoint in smartPoints)
        {
            lines.Add(new TrackingLine(
                smartPoint.Position,
                HorizontalDirection,
                TrackingLineKind.Horizontal,
                smartPoint));

            lines.Add(new TrackingLine(
                smartPoint.Position,
                VerticalDirection,
                TrackingLineKind.Vertical,
                smartPoint));
        }

        return lines;
    }

    public IReadOnlyList<TrackingLine> BuildLines(
        IEnumerable<SmartPoint> smartPoints,
        CadDocument? document = null)
    {
        ArgumentNullException.ThrowIfNull(smartPoints);

        var points = smartPoints.ToList();
        var lines = new List<TrackingLine>(BuildAxisLines(points));

        if (document is null)
        {
            return lines;
        }

        foreach (SmartPoint smartPoint in points)
        {
            AddEntityExtensionLines(
                document,
                smartPoint,
                lines);
        }

        return lines;
    }

    public SnapCandidate? FindNearestTrackingCandidate(
        IEnumerable<SmartPoint> smartPoints,
        Point2D cursorPoint,
        double tolerance,
        CadDocument? document = null)
    {
        if (tolerance <= 0)
        {
            return null;
        }

        IReadOnlyList<TrackingLine> lines = BuildLines(
            smartPoints,
            document);

        SnapCandidate? intersectionCandidate = FindNearestTrackingIntersectionCandidate(
            lines,
            cursorPoint,
            tolerance);

        if (intersectionCandidate is not null)
        {
            return intersectionCandidate;
        }

        SnapCandidate? bestCandidate = null;

        foreach (TrackingLine line in lines)
        {
            Point2D projected = Project(cursorPoint, line);
            double distance = cursorPoint.DistanceTo(projected);

            if (distance > tolerance)
            {
                continue;
            }

            if (bestCandidate is null || distance < bestCandidate.DistanceToCursor)
            {
                Vector2D signedDirection = ResolveSignedDirection(
                    cursorPoint,
                    line);

                bestCandidate = new SnapCandidate(
                    line.Kind == TrackingLineKind.EntityExtension
                        ? SnapKind.Extension
                        : SnapKind.Tracking,
                    projected,
                    line.SourcePoint.SourceEntityId,
                    distance,
                    line.Origin,
                    signedDirection);
            }
        }

        return bestCandidate;
    }

    private static void AddEntityExtensionLines(
        CadDocument document,
        SmartPoint smartPoint,
        List<TrackingLine> lines)
    {
        if (!smartPoint.SourceEntityId.HasValue ||
            !document.Entities.TryGet(smartPoint.SourceEntityId.Value, out CadEntity? entity) ||
            entity is null ||
            !document.IsEntityVisible(entity))
        {
            return;
        }

        switch (entity)
        {
            case LineEntity line:
                AddLineExtension(
                    smartPoint,
                    line.Start,
                    line.End,
                    lines);
                break;

            case PolylineEntity polyline:
                AddPolylineExtensionLines(
                    smartPoint,
                    polyline,
                    lines);
                break;
        }
    }

    private static void AddPolylineExtensionLines(
        SmartPoint smartPoint,
        PolylineEntity polyline,
        List<TrackingLine> lines)
    {
        for (int index = 0; index < polyline.SegmentCount; index++)
        {
            if (!Tolerance.IsZero(polyline.SegmentBulges[index]))
            {
                continue;
            }

            Point2D start = polyline.Vertices[index];
            Point2D end = polyline.Vertices[(index + 1) % polyline.Vertices.Count];

            if (IsSmartPointOnSegmentReference(
                    smartPoint.Position,
                    start,
                    end))
            {
                AddLineExtension(
                    smartPoint,
                    start,
                    end,
                    lines);
            }
        }
    }

    private static bool IsSmartPointOnSegmentReference(
        Point2D smartPoint,
        Point2D start,
        Point2D end)
    {
        if (Tolerance.ArePointsEqual(smartPoint, start) ||
            Tolerance.ArePointsEqual(smartPoint, end))
        {
            return true;
        }

        Point2D midpoint = new LineSegment2D(start, end).Midpoint;
        return Tolerance.ArePointsEqual(smartPoint, midpoint);
    }

    private static void AddLineExtension(
        SmartPoint smartPoint,
        Point2D start,
        Point2D end,
        List<TrackingLine> lines)
    {
        Vector2D direction = start.VectorTo(end);

        if (direction.LengthSquared <= 0)
        {
            return;
        }

        TrackingLine candidate = new(
            smartPoint.Position,
            direction,
            TrackingLineKind.EntityExtension,
            smartPoint);

        if (ContainsEquivalentLine(lines, candidate))
        {
            return;
        }

        lines.Add(candidate);
    }

    private static bool ContainsEquivalentLine(
        IEnumerable<TrackingLine> lines,
        TrackingLine candidate)
    {
        foreach (TrackingLine line in lines)
        {
            if (!ReferenceEquals(line.SourcePoint, candidate.SourcePoint))
            {
                continue;
            }

            if (line.Kind != candidate.Kind)
            {
                continue;
            }

            if (Math.Abs(line.Direction.Cross(candidate.Direction)) <= ParallelTolerance &&
                line.Direction.Dot(candidate.Direction) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static SnapCandidate? FindNearestTrackingIntersectionCandidate(
        IReadOnlyList<TrackingLine> lines,
        Point2D cursorPoint,
        double tolerance)
    {
        SnapCandidate? bestCandidate = null;

        for (int i = 0; i < lines.Count; i++)
        {
            for (int j = i + 1; j < lines.Count; j++)
            {
                TrackingLine first = lines[i];
                TrackingLine second = lines[j];

                if (ReferenceEquals(first.SourcePoint, second.SourcePoint))
                {
                    continue;
                }

                Point2D? intersection = TryIntersect(first, second);

                if (intersection is null)
                {
                    continue;
                }

                double distance = cursorPoint.DistanceTo(intersection.Value);

                if (distance > tolerance)
                {
                    continue;
                }

                if (bestCandidate is null || distance < bestCandidate.DistanceToCursor)
                {
                    bestCandidate = new SnapCandidate(
                        SnapKind.TrackingIntersection,
                        intersection.Value,
                        null,
                        distance);
                }
            }
        }

        return bestCandidate;
    }

    private static Point2D? TryIntersect(
        TrackingLine first,
        TrackingLine second)
    {
        double denominator = first.Direction.Cross(second.Direction);

        if (Math.Abs(denominator) <= ParallelTolerance)
        {
            return null;
        }

        Vector2D betweenOrigins = first.Origin.VectorTo(second.Origin);
        double firstDistance = betweenOrigins.Cross(second.Direction) / denominator;

        return first.Origin + first.Direction * firstDistance;
    }

    private static Vector2D ResolveSignedDirection(
        Point2D cursorPoint,
        TrackingLine line)
    {
        Vector2D fromOrigin = line.Origin.VectorTo(cursorPoint);
        return fromOrigin.Dot(line.Direction) < 0
            ? line.Direction * -1.0
            : line.Direction;
    }

    private static Point2D Project(
        Point2D point,
        TrackingLine line)
    {
        Vector2D fromOrigin = line.Origin.VectorTo(point);
        double distanceAlongLine = fromOrigin.Dot(line.Direction);
        return line.Origin + line.Direction * distanceAlongLine;
    }
}
