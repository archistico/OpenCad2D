using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Interaction.Selection;

/// <summary>
/// Represents the current set of selected entity ids.
/// This is UI/interaction state, not persistent CAD document data.
/// </summary>
public sealed class SelectionSet
{
    private readonly HashSet<EntityId> _selectedIds = new();
    private readonly List<EntityId> _selectionOrder = new();

    public IReadOnlyCollection<EntityId> SelectedIds => _selectionOrder;

    public int Count => _selectedIds.Count;

    public bool IsEmpty => _selectedIds.Count == 0;

    /// <summary>
    /// Gets the last entity that became selected.
    /// This is used by grip editing when multiple entities are selected.
    /// </summary>
    public EntityId? LastSelectedId => _selectionOrder.Count == 0
        ? null
        : _selectionOrder[^1];

    public bool Contains(EntityId entityId)
    {
        return _selectedIds.Contains(entityId);
    }

    public void Select(EntityId entityId)
    {
        if (!_selectedIds.Add(entityId))
        {
            return;
        }

        _selectionOrder.Add(entityId);
    }

    public void SelectMany(IEnumerable<EntityId> entityIds)
    {
        ArgumentNullException.ThrowIfNull(entityIds);

        foreach (EntityId entityId in entityIds)
        {
            Select(entityId);
        }
    }

    public void Deselect(EntityId entityId)
    {
        if (!_selectedIds.Remove(entityId))
        {
            return;
        }

        _selectionOrder.Remove(entityId);
    }

    public void Toggle(EntityId entityId)
    {
        if (_selectedIds.Contains(entityId))
        {
            Deselect(entityId);
            return;
        }

        Select(entityId);
    }

    public void ReplaceWith(EntityId entityId)
    {
        Clear();
        Select(entityId);
    }

    public void ReplaceWith(IEnumerable<EntityId> entityIds)
    {
        ArgumentNullException.ThrowIfNull(entityIds);

        Clear();
        SelectMany(entityIds);
    }

    public void Clear()
    {
        _selectedIds.Clear();
        _selectionOrder.Clear();
    }
}
