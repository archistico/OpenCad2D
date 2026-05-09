using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides nearest snap candidates on visible entities.
/// </summary>
public sealed class NearestSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Nearest;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        foreach (CadEntity entity in request.Document.GetVisibleEntities(request.SearchArea))
        {
            Point2D closestPoint = entity.GetClosestPoint(request.CursorPoint);
            double distance = request.CursorPoint.DistanceTo(closestPoint);

            if (distance <= request.Tolerance)
            {
                yield return new SnapCandidate(
                    SnapKind.Nearest,
                    closestPoint,
                    entity.Id,
                    distance);
            }
        }
    }
}