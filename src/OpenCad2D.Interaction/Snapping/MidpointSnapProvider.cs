using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides midpoint snap candidates.
/// </summary>
public sealed class MidpointSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Midpoint;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        foreach (CadEntity entity in request.Document.Entities.All.Where(entity => entity.IsVisible))
        {
            foreach (Point2D point in GetEntityMidpoints(entity))
            {
                double distance = request.CursorPoint.DistanceTo(point);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Midpoint,
                        point,
                        entity.Id,
                        distance);
                }
            }
        }
    }

    private static IEnumerable<Point2D> GetEntityMidpoints(CadEntity entity)
    {
        switch (entity)
        {
            case LineEntity line:
                yield return line.Geometry.Midpoint;
                break;

            case PolylineEntity polyline:
                foreach (LineSegment2D segment in polyline.Geometry.GetSegments())
                {
                    yield return segment.Midpoint;
                }

                break;

            case ArcEntity arc:
                yield return GetArcMidpoint(arc.Geometry);
                break;
        }
    }

    private static Point2D GetArcMidpoint(Arc2D arc)
    {
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;

        double sweep;

        if (arc.IsCounterClockwise)
        {
            if (end < start)
            {
                end += 2.0 * Math.PI;
            }

            sweep = end - start;
            return arc.PointAt(Angle.FromRadians(start + sweep / 2.0));
        }

        if (start < end)
        {
            start += 2.0 * Math.PI;
        }

        sweep = start - end;
        return arc.PointAt(Angle.FromRadians(start - sweep / 2.0));
    }
}