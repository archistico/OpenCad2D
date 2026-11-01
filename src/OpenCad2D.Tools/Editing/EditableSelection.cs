using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Resolves the part of the current selection that an editing command may change.
/// A selection can still contain entities that were deleted (for example by undo) or whose
/// layer was hidden or locked after they were selected; transforming those would either throw
/// half-way through a command or silently edit geometry the user cannot see or must not change.
/// </summary>
internal static class EditableSelection
{
    public static bool HasAny(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Selection.SelectedIds.Any(id => IsEditable(context, id));
    }

    public static IReadOnlyList<CadEntity> GetEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entities = new List<CadEntity>();
        foreach (EntityId id in context.Selection.SelectedIds)
        {
            if (TryGetEditable(context, id, out CadEntity? entity))
            {
                entities.Add(entity!);
            }
        }

        return entities;
    }

    public static IReadOnlyList<EntityId> GetIds(ToolContext context)
    {
        return GetEntities(context)
            .Select(entity => entity.Id)
            .ToList();
    }

    /// <summary>
    /// Removes deleted, hidden and locked entities from the selection and returns how many were removed.
    /// </summary>
    public static int Prune(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        int removed = 0;
        foreach (EntityId id in context.Selection.SelectedIds.ToArray())
        {
            if (IsEditable(context, id))
            {
                continue;
            }

            context.Selection.Set.Deselect(id);
            removed++;
        }

        return removed;
    }

    public static string FormatSkippedSuffix(int skippedCount)
    {
        return skippedCount switch
        {
            <= 0 => string.Empty,
            1 => " 1 selected entity that was deleted or is on a hidden or locked layer was skipped.",
            _ => $" {skippedCount} selected entities that were deleted or are on hidden or locked layers were skipped."
        };
    }

    private static bool IsEditable(ToolContext context, EntityId id)
    {
        return TryGetEditable(context, id, out _);
    }

    private static bool TryGetEditable(ToolContext context, EntityId id, out CadEntity? entity)
    {
        if (context.Document.Entities.TryGet(id, out entity) &&
            entity is not null &&
            context.Document.IsEntitySelectable(entity))
        {
            return true;
        }

        entity = null;
        return false;
    }
}
