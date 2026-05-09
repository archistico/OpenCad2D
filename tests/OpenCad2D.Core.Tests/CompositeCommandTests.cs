using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CompositeCommandTests
{
    [Fact]
    public void Execute_ShouldExecuteAllChildCommands()
    {
        var document = new CadDocument();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var secondLine = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 10));

        var command = new CompositeCommand(
            "Add two lines",
            new ICadCommand[]
            {
                new AddEntityCommand(firstLine),
                new AddEntityCommand(secondLine)
            });

        command.Execute(document);

        Assert.Equal(2, document.Entities.Count);
        Assert.True(document.Entities.Contains(firstLine.Id));
        Assert.True(document.Entities.Contains(secondLine.Id));
    }

    [Fact]
    public void Undo_ShouldUndoChildCommandsInReverseOrder()
    {
        var document = new CadDocument();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var secondLine = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 10));

        var command = new CompositeCommand(
            "Add two lines",
            new ICadCommand[]
            {
                new AddEntityCommand(firstLine),
                new AddEntityCommand(secondLine)
            });

        command.Execute(document);
        command.Undo(document);

        Assert.Equal(0, document.Entities.Count);
    }

    [Fact]
    public void CommandHistory_ShouldTreatCompositeCommandAsSingleUndoStep()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var secondLine = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 10));

        var command = new CompositeCommand(
            "Add two lines",
            new ICadCommand[]
            {
                new AddEntityCommand(firstLine),
                new AddEntityCommand(secondLine)
            });

        history.Execute(document, command);

        Assert.Equal(2, document.Entities.Count);
        Assert.Equal(1, history.UndoCount);

        history.Undo(document);

        Assert.Equal(0, document.Entities.Count);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(1, history.RedoCount);
    }

    [Fact]
    public void Redo_ShouldRedoCompositeCommandAsSingleStep()
    {
        var document = new CadDocument();
        var history = new CommandHistory();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var secondLine = new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 10));

        var command = new CompositeCommand(
            "Add two lines",
            new ICadCommand[]
            {
                new AddEntityCommand(firstLine),
                new AddEntityCommand(secondLine)
            });

        history.Execute(document, command);
        history.Undo(document);
        history.Redo(document);

        Assert.Equal(2, document.Entities.Count);
        Assert.True(document.Entities.Contains(firstLine.Id));
        Assert.True(document.Entities.Contains(secondLine.Id));
    }
}