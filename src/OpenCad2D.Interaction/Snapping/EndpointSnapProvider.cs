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
        foreach (CadEntity entity in request.Document.GetVisibleEntities(request.SearchArea))
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
            case PointEntity point:
                yield return point.Position;
                break;

            case TextEntity text:
                yield return text.InsertionPoint;
                break;

            case MultilineTextEntity multilineText:
                yield return multilineText.InsertionPoint;
                break;

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

            case BezierSplineEntity spline:
                foreach (Point2D point in spline.ControlPoints)
                {
                    yield return point;
                }

                break;

            case ArcEntity arc:
                yield return arc.Geometry.StartPoint;
                yield return arc.Geometry.EndPoint;
                break;
        }
    }
}