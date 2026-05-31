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

    public SnapCandidate? FindNearestTrackingCandidate(
        IEnumerable<SmartPoint> smartPoints,
        Point2D cursorPoint,
        double tolerance)
    {
        if (tolerance <= 0)
        {
            return null;
        }

        IReadOnlyList<TrackingLine> lines = BuildAxisLines(smartPoints);

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
                    SnapKind.Tracking,
                    projected,
                    null,
                    distance,
                    line.Origin,
                    signedDirection);
            }
        }

        return bestCandidate;
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
