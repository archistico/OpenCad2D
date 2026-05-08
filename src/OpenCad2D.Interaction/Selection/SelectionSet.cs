using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Interaction.Selection;

/// <summary>
/// Represents the current set of selected entity ids.
/// This is UI/interaction state, not persistent CAD document data.
/// </summary>
public sealed class SelectionSet
{
    private readonly HashSet<EntityId> _selectedIds = new();

    public IReadOnlyCollection<EntityId> SelectedIds => _selectedIds;

    public int Count => _selectedIds.Count;

    public bool IsEmpty => _selectedIds.Count == 0;

    public bool Contains(EntityId entityId)
    {
        return _selectedIds.Contains(entityId);
    }

    public void Select(EntityId entityId)
    {
        _selectedIds.Add(entityId);
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
        _selectedIds.Remove(entityId);
    }

    public void Toggle(EntityId entityId)
    {
        if (!_selectedIds.Add(entityId))
        {
            _selectedIds.Remove(entityId);
        }
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
    }
}