using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.HitTesting;

/// <summary>
/// Provides hit testing against CAD entities.
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

        return document.GetVisibleEntities()
            .Select(entity => CreateResult(entity, point))
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

        return document.GetVisibleEntities()
            .Select(entity => CreateResult(entity, point))
            .Where(result => result.Distance <= tolerance)
            .OrderBy(result => result.Distance)
            .ThenByDescending(result => result.Entity.DrawOrder)
            .ToList();
    }

    private static HitTestResult CreateResult(
        CadEntity entity,
        Point2D point)
    {
        Point2D closestPoint = entity.GetClosestPoint(point);
        double distance = point.DistanceTo(closestPoint);

        return new HitTestResult(
            entity,
            closestPoint,
            distance);
    }
}