using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides quadrant snap candidates for circles and arcs.
/// </summary>
public sealed class QuadrantSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Quadrant;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        foreach (CadEntity entity in request.Document.GetVisibleEntities(request.SearchArea))
        {
            foreach (Point2D point in GetQuadrantPoints(entity))
            {
                double distance = request.CursorPoint.DistanceTo(point);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Quadrant,
                        point,
                        entity.Id,
                        distance);
                }
            }
        }
    }

    private static IEnumerable<Point2D> GetQuadrantPoints(CadEntity entity)
    {
        Angle[] quadrantAngles =
        {
            Angle.FromDegrees(0),
            Angle.FromDegrees(90),
            Angle.FromDegrees(180),
            Angle.FromDegrees(270)
        };

        switch (entity)
        {
            case CircleEntity circle:
                foreach (Angle angle in quadrantAngles)
                {
                    yield return circle.Geometry.PointAt(angle);
                }

                break;

            case ArcEntity arc:
                foreach (Angle angle in quadrantAngles)
                {
                    Point2D point = arc.Geometry.PointAt(angle);

                    if (arc.Geometry.ContainsPoint(point))
                    {
                        yield return point;
                    }
                }

                break;
        }
    }
}