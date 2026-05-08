using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class TransformEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldTransformEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new TransformEntitiesCommand(
            new[] { line.Id },
            Matrix2D.Translation(5, 2));

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

        var command = new TransformEntitiesCommand(
            new[] { line.Id },
            Matrix2D.Translation(5, 2));

        command.Execute(document);
        command.Undo(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), result.Start);
        Assert.Equal(new Point2D(10, 0), result.End);
    }

    [Fact]
    public void Execute_WithMultipleEntities_ShouldTransformAllEntities()
    {
        var document = new CadDocument();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new CircleEntity(
            new Point2D(0, 0),
            5);

        document.AddEntity(first);
        document.AddEntity(second);

        var command = new TransformEntitiesCommand(
            new[] { first.Id, second.Id },
            Matrix2D.Translation(10, 0));

        command.Execute(document);

        var transformedFirst = (LineEntity)document.Entities.GetRequired(first.Id);
        var transformedSecond = (CircleEntity)document.Entities.GetRequired(second.Id);

        Assert.Equal(new Point2D(10, 0), transformedFirst.Start);
        Assert.Equal(new Point2D(20, 0), transformedFirst.End);
        Assert.Equal(new Point2D(10, 0), transformedSecond.Center);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new TransformEntitiesCommand(
                Array.Empty<OpenCad2D.Core.Identifiers.EntityId>(),
                Matrix2D.Identity));
    }
}