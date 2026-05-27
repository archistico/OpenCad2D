using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Interaction.BlockReferences;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.HitTesting;

/// <summary>
/// Provides hit testing against selectable CAD entities.
/// </summary>
public sealed class HitTestService
{
    public HitTestResult? HitTest(
        CadDocument document,
        Point2D point,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Hit test tolerance cannot be negative.");
        }

        BoundingBox2D searchArea = CreateSearchArea(point, tolerance);

        return document.GetSelectableEntities(searchArea)
            .Select(entity => CreateResult(document, entity, point))
            .Where(result => result.Distance <= tolerance)
            .OrderBy(result => result.Distance)
            .ThenByDescending(result => result.Entity.DrawOrder)
            .FirstOrDefault();
    }

    public IReadOnlyList<HitTestResult> HitTestAll(
        CadDocument document,
        Point2D point,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Hit test tolerance cannot be negative.");
        }

        BoundingBox2D searchArea = CreateSearchArea(point, tolerance);

        return document.GetSelectableEntities(searchArea)
            .Select(entity => CreateResult(document, entity, point))
            .Where(result => result.Distance <= tolerance)
            .OrderBy(result => result.Distance)
            .ThenByDescending(result => result.Entity.DrawOrder)
            .ToList();
    }

    private static HitTestResult CreateResult(
        CadDocument document,
        CadEntity entity,
        Point2D point)
    {
        if (entity is BlockReferenceEntity blockReference)
        {
            return CreateBlockReferenceResult(
                document,
                blockReference,
                point);
        }

        Point2D closestPoint = entity.GetClosestPoint(point);
        double distance = point.DistanceTo(closestPoint);

        return new HitTestResult(
            entity,
            closestPoint,
            distance);
    }

    private static HitTestResult CreateBlockReferenceResult(
        CadDocument document,
        BlockReferenceEntity blockReference,
        Point2D point)
    {
        Point2D closestPoint = blockReference.GetClosestPoint(point);
        double bestDistance = point.DistanceTo(closestPoint);

        foreach (CadEntity worldEntity in BlockReferenceGeometryResolver.GetWorldEntities(
            document,
            blockReference))
        {
            Point2D candidate = worldEntity.GetClosestPoint(point);
            double distance = point.DistanceTo(candidate);

            if (distance < bestDistance)
            {
                closestPoint = candidate;
                bestDistance = distance;
            }
        }

        return new HitTestResult(
            blockReference,
            closestPoint,
            bestDistance);
    }

    private static BoundingBox2D CreateSearchArea(
        Point2D point,
        double tolerance)
    {
        return new BoundingBox2D(
            point.X - tolerance,
            point.Y - tolerance,
            point.X + tolerance,
            point.Y + tolerance);
    }
}