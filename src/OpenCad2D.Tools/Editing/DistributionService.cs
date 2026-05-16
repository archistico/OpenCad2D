using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Tools.Editing;

public sealed class DistributionService
{
    public IReadOnlyList<CadEntity> CreateDistributedEntities(
        CadDocument document,
        IEnumerable<EntityId> selectedIds,
        DistributionOperation operation)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selectedIds);

        var selectedEntities = new List<CadEntity>();

        foreach (EntityId id in selectedIds)
        {
            if (!document.Entities.TryGet(id, out CadEntity? entity) || entity is null)
            {
                continue;
            }

            if (!document.IsEntitySelectable(entity))
            {
                continue;
            }

            selectedEntities.Add(entity);
        }

        if (selectedEntities.Count < 3)
        {
            return Array.Empty<CadEntity>();
        }

        List<CadEntity> ordered = operation switch
        {
            DistributionOperation.Horizontal => selectedEntities
                .OrderBy(entity => entity.GetBoundingBox().Center.X)
                .ThenBy(entity => entity.DrawOrder)
                .ThenBy(entity => entity.Id.Value)
                .ToList(),

            DistributionOperation.Vertical => selectedEntities
                .OrderBy(entity => entity.GetBoundingBox().Center.Y)
                .ThenBy(entity => entity.DrawOrder)
                .ThenBy(entity => entity.Id.Value)
                .ToList(),

            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        double firstCenter = GetCenterCoordinate(ordered[0], operation);
        double lastCenter = GetCenterCoordinate(ordered[^1], operation);
        double step = (lastCenter - firstCenter) / (ordered.Count - 1);

        var replacements = new List<CadEntity>();

        for (int index = 1; index < ordered.Count - 1; index++)
        {
            CadEntity entity = ordered[index];
            double targetCenter = firstCenter + (step * index);
            double currentCenter = GetCenterCoordinate(entity, operation);
            double delta = targetCenter - currentCenter;

            if (Math.Abs(delta) <= Tolerance.Default)
            {
                continue;
            }

            Matrix2D transform = operation switch
            {
                DistributionOperation.Horizontal => Matrix2D.Translation(delta, 0),
                DistributionOperation.Vertical => Matrix2D.Translation(0, delta),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            replacements.Add(entity.Transform(transform));
        }

        return replacements;
    }

    private static double GetCenterCoordinate(
        CadEntity entity,
        DistributionOperation operation)
    {
        BoundingBox2D bounds = entity.GetBoundingBox();

        return operation switch
        {
            DistributionOperation.Horizontal => bounds.Center.X,
            DistributionOperation.Vertical => bounds.Center.Y,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }
}
