using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class AddEntityCommandTests
{
    [Fact]
    public void Execute_ShouldAddEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var command = new AddEntityCommand(line);

        command.Execute(document);

        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void Undo_ShouldRemoveEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var command = new AddEntityCommand(line);

        command.Execute(document);
        command.Undo(document);

        Assert.False(document.Entities.Contains(line.Id));
        Assert.Equal(0, document.Entities.Count);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new AddEntityCommand(Array.Empty<CadEntity>()));
    }
}