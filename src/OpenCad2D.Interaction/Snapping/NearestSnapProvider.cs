using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.BlockReferences;

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
            foreach ((Point2D point, EntityId entityId) in GetCandidatePoints(request, entity))
            {
                double distance = request.CursorPoint.DistanceTo(point);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Nearest,
                        point,
                        entityId,
                        distance);
                }
            }
        }
    }

    private static IEnumerable<(Point2D Point, EntityId EntityId)> GetCandidatePoints(
        SnapRequest request,
        CadEntity entity)
    {
        if (entity is BlockReferenceEntity blockReference)
        {
            foreach (CadEntity worldEntity in BlockReferenceGeometryResolver.GetWorldEntities(
                request.Document,
                blockReference))
            {
                yield return (worldEntity.GetClosestPoint(request.CursorPoint), blockReference.Id);
            }

            yield break;
        }

        yield return (entity.GetClosestPoint(request.CursorPoint), entity.Id);
    }
}
