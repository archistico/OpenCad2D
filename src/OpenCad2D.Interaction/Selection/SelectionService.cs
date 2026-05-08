using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.HitTesting;

namespace OpenCad2D.Interaction.Selection;

/// <summary>
/// Provides entity selection operations.
/// </summary>
public sealed class SelectionService
{
    private readonly HitTestService _hitTestService;

    public SelectionService()
        : this(new HitTestService())
    {
    }

    public SelectionService(HitTestService hitTestService)
    {
        _hitTestService = hitTestService;
    }

    public EntityId? SelectByPoint(
        CadDocument document,
        Point2D point,
        double tolerance)
    {
        HitTestResult? result = _hitTestService.HitTest(
            document,
            point,
            tolerance);

        return result?.Entity.Id;
    }

    public IReadOnlyList<EntityId> SelectByWindow(
        CadDocument document,
        BoundingBox2D window,
        WindowSelectionMode mode)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.Entities.All
            .Where(entity => entity.IsVisible)
            .Where(entity => MatchesWindow(entity, window, mode))
            .OrderBy(entity => entity.DrawOrder)
            .Select(entity => entity.Id)
            .ToList();
    }

    private static bool MatchesWindow(
        CadEntity entity,
        BoundingBox2D window,
        WindowSelectionMode mode)
    {
        BoundingBox2D entityBounds = entity.GetBoundingBox();

        return mode switch
        {
            WindowSelectionMode.Inside =>
                window.Contains(entityBounds),

            WindowSelectionMode.Crossing =>
                window.Intersects(entityBounds),

            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }
}