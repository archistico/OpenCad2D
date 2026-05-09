using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Operations;
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

        return document.GetVisibleEntities(window)
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
                MatchesCrossingWindow(entity, window),

            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static bool MatchesCrossingWindow(
        CadEntity entity,
        BoundingBox2D window)
    {
        if (!window.Intersects(entity.GetBoundingBox()))
        {
            return false;
        }

        return entity switch
        {
            LineEntity line =>
                RectangleIntersectionService.IntersectsSegment(
                    window,
                    line.Geometry),

            PolylineEntity polyline =>
                RectangleIntersectionService.IntersectsPolyline(
                    window,
                    polyline.Geometry),

            CircleEntity circle =>
                RectangleIntersectionService.IntersectsCircle(
                    window,
                    circle.Geometry),

            ArcEntity arc =>
                RectangleIntersectionService.IntersectsArc(
                    window,
                    arc.Geometry),

            _ => window.Intersects(entity.GetBoundingBox())
        };
    }
}