using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CommandHistoryTests
{
    [Fact]
    public void Constructor_ShouldStartEmpty()
    {
        var history = new CommandHistory();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    [Fact]
    public void Execute_ShouldRunCommandAndAddItToUndoStack()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        history.Execute(document, new AddEntityCommand(line));

        Assert.Equal(1, document.Entities.Count);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_ShouldUndoLastCommand()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        history.Execute(document, new AddEntityCommand(line));

        history.Undo(document);

        Assert.Equal(0, document.Entities.Count);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Redo_ShouldRedoLastUndoneCommand()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        history.Execute(document, new AddEntityCommand(line));
        history.Undo(document);
        history.Redo(document);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Execute_AfterUndo_ShouldClearRedoStack()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 10));

        history.Execute(document, new AddEntityCommand(first));
        history.Undo(document);

        history.Execute(document, new AddEntityCommand(second));

        Assert.False(history.CanRedo);
        Assert.True(document.Entities.Contains(second.Id));
        Assert.False(document.Entities.Contains(first.Id));
    }

    [Fact]
    public void Undo_WhenNoCommandExists_ShouldThrow()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        Assert.Throws<InvalidOperationException>(() =>
            history.Undo(document));
    }

    [Fact]
    public void Redo_WhenNoCommandExists_ShouldThrow()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        Assert.Throws<InvalidOperationException>(() =>
            history.Redo(document));
    }
}