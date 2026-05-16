using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Tools.Editing;

public sealed class AlignmentService
{
    public IReadOnlyList<CadEntity> CreateAlignedEntities(
        CadDocument document,
        IEnumerable<EntityId> selectedIds,
        AlignmentOperation operation)
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

        if (selectedEntities.Count < 2)
        {
            return Array.Empty<CadEntity>();
        }

        BoundingBox2D selectionBounds = GetCombinedBounds(selectedEntities);
        var replacements = new List<CadEntity>();

        foreach (CadEntity entity in selectedEntities)
        {
            BoundingBox2D bounds = entity.GetBoundingBox();
            double dx = 0;
            double dy = 0;

            switch (operation)
            {
                case AlignmentOperation.Left:
                    dx = selectionBounds.MinX - bounds.MinX;
                    break;

                case AlignmentOperation.Right:
                    dx = selectionBounds.MaxX - bounds.MaxX;
                    break;

                case AlignmentOperation.Top:
                    dy = selectionBounds.MinY - bounds.MinY;
                    break;

                case AlignmentOperation.Bottom:
                    dy = selectionBounds.MaxY - bounds.MaxY;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }

            if (Math.Abs(dx) <= Tolerance.Default && Math.Abs(dy) <= Tolerance.Default)
            {
                continue;
            }

            replacements.Add(entity.Transform(Matrix2D.Translation(dx, dy)));
        }

        return replacements;
    }

    private static BoundingBox2D GetCombinedBounds(IReadOnlyList<CadEntity> entities)
    {
        BoundingBox2D first = entities[0].GetBoundingBox();
        double minX = first.MinX;
        double minY = first.MinY;
        double maxX = first.MaxX;
        double maxY = first.MaxY;

        for (int index = 1; index < entities.Count; index++)
        {
            BoundingBox2D bounds = entities[index].GetBoundingBox();
            minX = Math.Min(minX, bounds.MinX);
            minY = Math.Min(minY, bounds.MinY);
            maxX = Math.Max(maxX, bounds.MaxX);
            maxY = Math.Max(maxY, bounds.MaxY);
        }

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }
}
