using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides center snap candidates.
/// </summary>
public sealed class CenterSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Center;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        foreach (CadEntity entity in request.Document.GetVisibleEntities(request.SearchArea))
        {
            Point2D? center = entity switch
            {
                CircleEntity circle => circle.Center,
                EllipseEntity ellipse => ellipse.Center,
                ArcEntity arc => arc.Center,
                _ => null
            };

            if (center is null)
            {
                continue;
            }

            double distance = request.CursorPoint.DistanceTo(center.Value);

            if (distance <= request.Tolerance)
            {
                yield return new SnapCandidate(
                    SnapKind.Center,
                    center.Value,
                    entity.Id,
                    distance);
            }
        }
    }
}