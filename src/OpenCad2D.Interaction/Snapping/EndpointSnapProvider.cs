using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides endpoint snap candidates.
/// </summary>
public sealed class EndpointSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Endpoint;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        foreach (CadEntity entity in request.Document.Entities.All.Where(entity => entity.IsVisible))
        {
            foreach (Point2D point in GetEntityEndpoints(entity))
            {
                double distance = request.CursorPoint.DistanceTo(point);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Endpoint,
                        point,
                        entity.Id,
                        distance);
                }
            }
        }
    }

    private static IEnumerable<Point2D> GetEntityEndpoints(CadEntity entity)
    {
        switch (entity)
        {
            case LineEntity line:
                yield return line.Start;
                yield return line.End;
                break;

            case PolylineEntity polyline:
                foreach (Point2D vertex in polyline.Vertices)
                {
                    yield return vertex;
                }

                break;

            case ArcEntity arc:
                yield return arc.Geometry.StartPoint;
                yield return arc.Geometry.EndPoint;
                break;
        }
    }
}