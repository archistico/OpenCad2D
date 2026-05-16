using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class ReplaceEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldReplaceEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var replacement = new LineEntity(
            new Point2D(5, 0),
            new Point2D(15, 0),
            id: line.Id);

        var command = new ReplaceEntitiesCommand(replacement);

        command.Execute(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 0), result.Start);
        Assert.Equal(new Point2D(15, 0), result.End);
    }

    [Fact]
    public void Undo_ShouldRestoreOriginalEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var replacement = new LineEntity(
            new Point2D(5, 0),
            new Point2D(15, 0),
            id: line.Id);

        var command = new ReplaceEntitiesCommand(replacement);

        command.Execute(document);
        command.Undo(document);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), result.Start);
        Assert.Equal(new Point2D(10, 0), result.End);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new ReplaceEntitiesCommand(Array.Empty<CadEntity>()));
    }

    [Fact]
    public void Execute_WhenRequested_ShouldMarkDimensionsAsStaleAndUndoShouldRestoreStatus()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var replacement = new LineEntity(
            new Point2D(0, 2),
            new Point2D(10, 2),
            id: line.Id);
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, -2),
            DimensionOrientation.Horizontal);

        document.AddEntity(line);
        document.AddEntity(dimension);

        var command = new ReplaceEntitiesCommand(
            replacement,
            markDimensionsStale: true);

        command.Execute(document);

        var staleDimension = Assert.IsType<LinearDimensionEntity>(
            document.Entities.GetRequired(dimension.Id));
        Assert.True(staleDimension.IsStale);

        command.Undo(document);

        var restoredDimension = Assert.IsType<LinearDimensionEntity>(
            document.Entities.GetRequired(dimension.Id));
        Assert.False(restoredDimension.IsStale);
    }

}