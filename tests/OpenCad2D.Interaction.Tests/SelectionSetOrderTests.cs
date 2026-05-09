using OpenCad2D.Core.Identifiers;
using OpenCad2D.Interaction.Selection;

namespace OpenCad2D.Interaction.Tests;

public sealed class SelectionSetOrderTests
{
    [Fact]
    public void LastSelectedId_WhenMultipleEntitiesAreSelected_ShouldReturnMostRecentSelection()
    {
        var selection = new SelectionSet();
        EntityId first = EntityId.New();
        EntityId second = EntityId.New();
        EntityId third = EntityId.New();

        selection.Select(first);
        selection.Select(second);
        selection.Select(third);

        Assert.Equal(third, selection.LastSelectedId);
    }

    [Fact]
    public void Toggle_WhenEntityIsAdded_ShouldUpdateLastSelectedId()
    {
        var selection = new SelectionSet();
        EntityId first = EntityId.New();
        EntityId second = EntityId.New();

        selection.Select(first);
        selection.Toggle(second);

        Assert.Equal(second, selection.LastSelectedId);
    }

    [Fact]
    public void Deselect_WhenLastEntityIsRemoved_ShouldExposePreviousSelectionAsLast()
    {
        var selection = new SelectionSet();
        EntityId first = EntityId.New();
        EntityId second = EntityId.New();

        selection.Select(first);
        selection.Select(second);
        selection.Deselect(second);

        Assert.Equal(first, selection.LastSelectedId);
    }
}
