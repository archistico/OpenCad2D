using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class DeleteEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldDeleteEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new DeleteEntitiesCommand(new[] { line.Id });

        command.Execute(document);

        Assert.False(document.Entities.Contains(line.Id));
        Assert.Equal(0, document.Entities.Count);
    }

    [Fact]
    public void Undo_ShouldRestoreDeletedEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new DeleteEntitiesCommand(new[] { line.Id });

        command.Execute(document);
        command.Undo(document);

        Assert.True(document.Entities.Contains(line.Id));
        Assert.Equal(1, document.Entities.Count);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new DeleteEntitiesCommand(Array.Empty<OpenCad2D.Core.Identifiers.EntityId>()));
    }
}