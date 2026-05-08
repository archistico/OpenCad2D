using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class MoveEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldMoveEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new MoveEntitiesCommand(
            new[] { line.Id },
            new Vector2D(5, 2));

        command.Execute(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 2), result.Start);
        Assert.Equal(new Point2D(15, 2), result.End);
    }

    [Fact]
    public void Undo_ShouldRestoreOriginalEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new MoveEntitiesCommand(
            new[] { line.Id },
            new Vector2D(5, 2));

        command.Execute(document);
        command.Undo(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), result.Start);
        Assert.Equal(new Point2D(10, 0), result.End);
    }
}