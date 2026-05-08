using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class RotateEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldRotateLineAroundOrigin()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(1, 0),
            new Point2D(2, 0));

        document.AddEntity(line);

        var command = new RotateEntitiesCommand(
            new[] { line.Id },
            Point2D.Origin,
            Angle.FromDegrees(90));

        command.Execute(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(0, result.Start.X, precision: 10);
        Assert.Equal(1, result.Start.Y, precision: 10);
        Assert.Equal(0, result.End.X, precision: 10);
        Assert.Equal(2, result.End.Y, precision: 10);
    }

    [Fact]
    public void Undo_ShouldRestoreOriginalEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(1, 0),
            new Point2D(2, 0));

        document.AddEntity(line);

        var command = new RotateEntitiesCommand(
            new[] { line.Id },
            Point2D.Origin,
            Angle.FromDegrees(90));

        command.Execute(document);
        command.Undo(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(1, 0), result.Start);
        Assert.Equal(new Point2D(2, 0), result.End);
    }
}