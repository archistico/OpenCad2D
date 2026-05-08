using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class MirrorEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldMirrorLineAcrossXAxis()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));

        document.AddEntity(line);

        Line2D mirrorLine = Line2D.FromPoints(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var command = new MirrorEntitiesCommand(
            new[] { line.Id },
            mirrorLine);

        command.Execute(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, -1), result.Start);
        Assert.Equal(new Point2D(10, -1), result.End);
    }

    [Fact]
    public void Undo_ShouldRestoreOriginalEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));

        document.AddEntity(line);

        Line2D mirrorLine = Line2D.FromPoints(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var command = new MirrorEntitiesCommand(
            new[] { line.Id },
            mirrorLine);

        command.Execute(document);
        command.Undo(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 1), result.Start);
        Assert.Equal(new Point2D(10, 1), result.End);
    }
}