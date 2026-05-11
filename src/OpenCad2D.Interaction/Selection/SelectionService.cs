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

    /// <summary>
    /// Returns all selectable entities hit by a point, ordered using the same hit-test priority
    /// used by point selection. This is useful when several entities overlap at the cursor.
    /// </summary>
    public IReadOnlyList<EntityId> SelectAllByPoint(
        CadDocument document,
        Point2D point,
        double tolerance)
    {
        return _hitTestService.HitTestAll(
                document,
                point,
                tolerance)
            .Select(result => result.Entity.Id)
            .ToList();
    }

    /// <summary>
    /// Returns the next selectable entity under the cursor after <paramref name="currentEntityId"/>.
    /// When the current entity is not under the cursor, the first hit entity is returned.
    /// </summary>
    public EntityId? SelectNextByPoint(
        CadDocument document,
        Point2D point,
        double tolerance,
        EntityId? currentEntityId)
    {
        IReadOnlyList<EntityId> ids = SelectAllByPoint(
            document,
            point,
            tolerance);

        if (ids.Count == 0)
        {
            return null;
        }

        if (currentEntityId is null)
        {
            return ids[0];
        }

        int currentIndex = -1;

        for (int index = 0; index < ids.Count; index++)
        {
            if (ids[index] == currentEntityId.Value)
            {
                currentIndex = index;
                break;
            }
        }

        if (currentIndex < 0)
        {
            return ids[0];
        }

        int nextIndex = (currentIndex + 1) % ids.Count;

        return ids[nextIndex];
    }

    public IReadOnlyList<EntityId> SelectByWindow(
        CadDocument document,
        BoundingBox2D window,
        WindowSelectionMode mode)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.GetSelectableEntities(window)
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