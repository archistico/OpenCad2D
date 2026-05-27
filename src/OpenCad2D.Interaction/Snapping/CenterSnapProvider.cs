using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.BlockReferences;

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
            foreach ((Point2D point, EntityId entityId) in GetCandidatePoints(request, entity))
            {
                double distance = request.CursorPoint.DistanceTo(point);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Center,
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
                Point2D? center = GetEntityCenter(worldEntity);

                if (center is not null)
                {
                    yield return (center.Value, blockReference.Id);
                }
            }

            yield break;
        }

        Point2D? entityCenter = GetEntityCenter(entity);

        if (entityCenter is not null)
        {
            yield return (entityCenter.Value, entity.Id);
        }
    }

    private static Point2D? GetEntityCenter(CadEntity entity)
    {
        return entity switch
        {
            CircleEntity circle => circle.Center,
            EllipseEntity ellipse => ellipse.Center,
            ArcEntity arc => arc.Center,
            ImageReferenceEntity imageReference => imageReference.Center,
            _ => null
        };
    }
}
