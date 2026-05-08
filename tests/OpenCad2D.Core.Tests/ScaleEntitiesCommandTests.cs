using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class ScaleEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldScaleLineAroundOrigin()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(1, 1),
            new Point2D(2, 1));

        document.AddEntity(line);

        var command = new ScaleEntitiesCommand(
            new[] { line.Id },
            Point2D.Origin,
            2);

        command.Execute(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(2, 2), result.Start);
        Assert.Equal(new Point2D(4, 2), result.End);
    }

    [Fact]
    public void Constructor_WithInvalidFactor_ShouldThrow()
    {
        var id = OpenCad2D.Core.Identifiers.EntityId.New();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScaleEntitiesCommand(
                new[] { id },
                Point2D.Origin,
                0));
    }

    [Fact]
    public void Undo_ShouldRestoreOriginalEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(1, 1),
            new Point2D(2, 1));

        document.AddEntity(line);

        var command = new ScaleEntitiesCommand(
            new[] { line.Id },
            Point2D.Origin,
            2);

        command.Execute(document);
        command.Undo(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(1, 1), result.Start);
        Assert.Equal(new Point2D(2, 1), result.End);
    }
}