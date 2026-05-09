using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CopyEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldCreateCopiedEntityWithNewId()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new CopyEntitiesCommand(
            new[] { line.Id },
            new Vector2D(5, 2));

        command.Execute(document);

        Assert.Equal(2, document.Entities.Count);
        Assert.Single(command.CreatedEntities);

        var copied = (LineEntity)command.CreatedEntities[0];

        Assert.NotEqual(line.Id, copied.Id);
        Assert.Equal(new Point2D(5, 2), copied.Start);
        Assert.Equal(new Point2D(15, 2), copied.End);
    }

    [Fact]
    public void Undo_ShouldRemoveCopiedEntityAndKeepOriginal()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new CopyEntitiesCommand(
            new[] { line.Id },
            new Vector2D(5, 2));

        command.Execute(document);
        command.Undo(document);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void Execute_WithMultipleEntities_ShouldCopyAll()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var circle = new CircleEntity(
            new Point2D(0, 0),
            5);

        document.AddEntity(line);
        document.AddEntity(circle);

        var command = new CopyEntitiesCommand(
            new[] { line.Id, circle.Id },
            new Vector2D(10, 0));

        command.Execute(document);

        Assert.Equal(4, document.Entities.Count);
        Assert.Equal(2, command.CreatedEntities.Count);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new CopyEntitiesCommand(
                Array.Empty<OpenCad2D.Core.Identifiers.EntityId>(),
                new Vector2D(1, 0)));
    }

    [Fact]
    public void Redo_ShouldReuseSameCreatedEntityIds()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new CopyEntitiesCommand(
            new[] { line.Id },
            new Vector2D(5, 0));

        history.Execute(document, command);

        var firstCreatedId = command.CreatedEntities.Single().Id;

        history.Undo(document);
        history.Redo(document);

        var secondCreatedId = command.CreatedEntities.Single().Id;

        Assert.Equal(firstCreatedId, secondCreatedId);
        Assert.True(document.Entities.Contains(firstCreatedId));
    }
}