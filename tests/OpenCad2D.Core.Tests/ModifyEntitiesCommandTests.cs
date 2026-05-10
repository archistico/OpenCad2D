using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class ModifyEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldRemoveOldEntityAndAddNewEntities()
    {
        var document = new CadDocument();

        var original = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(4, 0));

        var second = new LineEntity(
            new Point2D(6, 0),
            new Point2D(10, 0));

        document.AddEntity(original);

        var command = new ModifyEntitiesCommand(
            new[] { original },
            new[] { first, second },
            "Break line");

        command.Execute(document);

        Assert.False(document.Entities.Contains(original.Id));
        Assert.True(document.Entities.Contains(first.Id));
        Assert.True(document.Entities.Contains(second.Id));
        Assert.Equal(2, document.Entities.Count);
    }

    [Fact]
    public void Undo_ShouldRestoreOldEntityAndRemoveNewEntities()
    {
        var document = new CadDocument();

        var original = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(4, 0));

        var second = new LineEntity(
            new Point2D(6, 0),
            new Point2D(10, 0));

        document.AddEntity(original);

        var command = new ModifyEntitiesCommand(
            new[] { original },
            new[] { first, second });

        command.Execute(document);
        command.Undo(document);

        Assert.True(document.Entities.Contains(original.Id));
        Assert.False(document.Entities.Contains(first.Id));
        Assert.False(document.Entities.Contains(second.Id));
        Assert.Equal(1, document.Entities.Count);
    }

    [Fact]
    public void Constructor_WhenNoEntitiesAreProvided_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new ModifyEntitiesCommand(
                Array.Empty<CadEntity>(),
                Array.Empty<CadEntity>()));
    }
}
