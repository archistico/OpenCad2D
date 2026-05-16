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
    private readonly List<EntityId> _lastDeselectedSelectionOrder = new();

    public IReadOnlyCollection<EntityId> SelectedIds => _selectionOrder;

    /// <summary>
    /// Gets the most recent non-empty selection that was explicitly cleared.
    /// This is used by Select Last to restore the last effective user selection.
    /// </summary>
    public IReadOnlyCollection<EntityId> LastDeselectedSelectionIds => _lastDeselectedSelectionOrder;

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
        if (!_selectedIds.Contains(entityId))
        {
            return;
        }

        if (_selectedIds.Count == 1)
        {
            RememberCurrentSelection();
        }

        _selectedIds.Remove(entityId);
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
        ReplaceWith(new[] { entityId });
    }

    public void ReplaceWith(IEnumerable<EntityId> entityIds)
    {
        ArgumentNullException.ThrowIfNull(entityIds);

        List<EntityId> replacement = entityIds
            .Distinct()
            .ToList();

        if (replacement.Count == 0)
        {
            Clear();
            return;
        }

        _selectedIds.Clear();
        _selectionOrder.Clear();
        SelectMany(replacement);
    }

    public void Clear()
    {
        RememberCurrentSelection();

        _selectedIds.Clear();
        _selectionOrder.Clear();
    }

    public void Reset()
    {
        _selectedIds.Clear();
        _selectionOrder.Clear();
        _lastDeselectedSelectionOrder.Clear();
    }

    private void RememberCurrentSelection()
    {
        if (_selectionOrder.Count == 0)
        {
            return;
        }

        _lastDeselectedSelectionOrder.Clear();
        _lastDeselectedSelectionOrder.AddRange(_selectionOrder);
    }
}
