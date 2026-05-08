using OpenCad2D.Core.Identifiers;
using OpenCad2D.Interaction.Selection;

namespace OpenCad2D.Interaction.Tests;

public sealed class SelectionSetTests
{
    [Fact]
    public void Constructor_ShouldStartEmpty()
    {
        var selection = new SelectionSet();

        Assert.True(selection.IsEmpty);
        Assert.Equal(0, selection.Count);
    }

    [Fact]
    public void Select_ShouldAddEntityId()
    {
        var selection = new SelectionSet();
        EntityId id = EntityId.New();

        selection.Select(id);

        Assert.True(selection.Contains(id));
        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void Select_SameEntityTwice_ShouldNotDuplicate()
    {
        var selection = new SelectionSet();
        EntityId id = EntityId.New();

        selection.Select(id);
        selection.Select(id);

        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void Deselect_ShouldRemoveEntityId()
    {
        var selection = new SelectionSet();
        EntityId id = EntityId.New();

        selection.Select(id);
        selection.Deselect(id);

        Assert.False(selection.Contains(id));
        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void Toggle_WhenEntityIsNotSelected_ShouldSelectIt()
    {
        var selection = new SelectionSet();
        EntityId id = EntityId.New();

        selection.Toggle(id);

        Assert.True(selection.Contains(id));
    }

    [Fact]
    public void Toggle_WhenEntityIsSelected_ShouldDeselectIt()
    {
        var selection = new SelectionSet();
        EntityId id = EntityId.New();

        selection.Select(id);
        selection.Toggle(id);

        Assert.False(selection.Contains(id));
    }

    [Fact]
    public void ReplaceWith_ShouldClearPreviousSelection()
    {
        var selection = new SelectionSet();

        EntityId first = EntityId.New();
        EntityId second = EntityId.New();

        selection.Select(first);
        selection.ReplaceWith(second);

        Assert.False(selection.Contains(first));
        Assert.True(selection.Contains(second));
        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void Clear_ShouldRemoveAllSelectedIds()
    {
        var selection = new SelectionSet();

        selection.Select(EntityId.New());
        selection.Select(EntityId.New());

        selection.Clear();

        Assert.True(selection.IsEmpty);
    }
}