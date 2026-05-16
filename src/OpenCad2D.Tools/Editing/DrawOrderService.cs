using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Tools.Editing;

public enum DrawOrderOperation
{
    BringToFront,
    SendToBack,
    BringForward,
    SendBackward
}

/// <summary>
/// Calculates draw-order replacements for selected entities.
/// Draw order is independent from layers: higher DrawOrder renders on top.
/// </summary>
public sealed class DrawOrderService
{
    public IReadOnlyList<CadEntity> CreateReorderedEntities(
        CadDocument document,
        IEnumerable<EntityId> selectedIds,
        DrawOrderOperation operation)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selectedIds);

        HashSet<EntityId> selected = selectedIds.ToHashSet();

        if (selected.Count == 0 || document.Entities.Count <= 1)
        {
            return Array.Empty<CadEntity>();
        }

        List<CadEntity> orderedEntities = document.Entities.All
            .OrderBy(entity => entity.DrawOrder)
            .ThenBy(entity => entity.Id.Value)
            .ToList();

        HashSet<EntityId> selectableSelectedIds = orderedEntities
            .Where(entity => selected.Contains(entity.Id))
            .Where(document.IsEntitySelectable)
            .Select(entity => entity.Id)
            .ToHashSet();

        if (selectableSelectedIds.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        List<CadEntity> reordered = operation switch
        {
            DrawOrderOperation.BringToFront => BringToFront(orderedEntities, selectableSelectedIds),
            DrawOrderOperation.SendToBack => SendToBack(orderedEntities, selectableSelectedIds),
            DrawOrderOperation.BringForward => BringForward(orderedEntities, selectableSelectedIds),
            DrawOrderOperation.SendBackward => SendBackward(orderedEntities, selectableSelectedIds),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        return CreateDenseDrawOrderReplacements(orderedEntities, reordered);
    }

    private static List<CadEntity> BringToFront(
        IReadOnlyList<CadEntity> allEntities,
        HashSet<EntityId> selectedIds)
    {
        return allEntities
            .Where(entity => !selectedIds.Contains(entity.Id))
            .Concat(allEntities.Where(entity => selectedIds.Contains(entity.Id)))
            .ToList();
    }

    private static List<CadEntity> SendToBack(
        IReadOnlyList<CadEntity> allEntities,
        HashSet<EntityId> selectedIds)
    {
        return allEntities
            .Where(entity => selectedIds.Contains(entity.Id))
            .Concat(allEntities.Where(entity => !selectedIds.Contains(entity.Id)))
            .ToList();
    }

    private static List<CadEntity> BringForward(
        IReadOnlyList<CadEntity> allEntities,
        HashSet<EntityId> selectedIds)
    {
        List<CadEntity> reordered = allEntities.ToList();

        for (int index = reordered.Count - 2; index >= 0; index--)
        {
            CadEntity current = reordered[index];
            CadEntity next = reordered[index + 1];

            if (selectedIds.Contains(current.Id) && !selectedIds.Contains(next.Id))
            {
                reordered[index] = next;
                reordered[index + 1] = current;
            }
        }

        return reordered;
    }

    private static List<CadEntity> SendBackward(
        IReadOnlyList<CadEntity> allEntities,
        HashSet<EntityId> selectedIds)
    {
        List<CadEntity> reordered = allEntities.ToList();

        for (int index = 1; index < reordered.Count; index++)
        {
            CadEntity current = reordered[index];
            CadEntity previous = reordered[index - 1];

            if (selectedIds.Contains(current.Id) && !selectedIds.Contains(previous.Id))
            {
                reordered[index - 1] = current;
                reordered[index] = previous;
            }
        }

        return reordered;
    }

    private static IReadOnlyList<CadEntity> CreateDenseDrawOrderReplacements(
        IReadOnlyList<CadEntity> originalOrder,
        IReadOnlyList<CadEntity> reordered)
    {
        if (originalOrder.Select(entity => entity.Id).SequenceEqual(reordered.Select(entity => entity.Id)))
        {
            return Array.Empty<CadEntity>();
        }

        int startDrawOrder = originalOrder.Min(entity => entity.DrawOrder);
        var replacements = new List<CadEntity>();

        for (int index = 0; index < reordered.Count; index++)
        {
            CadEntity entity = reordered[index];
            int newDrawOrder = startDrawOrder + index;

            if (entity.DrawOrder != newDrawOrder)
            {
                replacements.Add(entity.WithDrawOrder(newDrawOrder));
            }
        }

        return replacements;
    }
}
