using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class DeleteEntitiesCommandTests
{
    [Fact]
    public void Execute_ShouldDeleteEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new DeleteEntitiesCommand(new[] { line.Id });

        command.Execute(document);

        Assert.False(document.Entities.Contains(line.Id));
        Assert.Equal(0, document.Entities.Count);
    }

    [Fact]
    public void Undo_ShouldRestoreDeletedEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var command = new DeleteEntitiesCommand(new[] { line.Id });

        command.Execute(document);
        command.Undo(document);

        Assert.True(document.Entities.Contains(line.Id));
        Assert.Equal(1, document.Entities.Count);
    }



    [Fact]
    public void Execute_WhenDeletingModelGeometry_ShouldMarkDimensionsStale()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, -2),
            DimensionOrientation.Horizontal);

        document.AddEntity(line);
        document.AddEntity(dimension);

        var command = new DeleteEntitiesCommand(new[] { line.Id });

        command.Execute(document);

        Assert.False(document.Entities.Contains(line.Id));
        var staleDimension = Assert.IsType<LinearDimensionEntity>(
            document.Entities.GetRequired(dimension.Id));
        Assert.True(staleDimension.IsStale);
    }

    [Fact]
    public void Undo_WhenDeletingModelGeometry_ShouldRestoreDeletedEntityAndDimensionStaleState()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, -2),
            DimensionOrientation.Horizontal);

        document.AddEntity(line);
        document.AddEntity(dimension);

        var command = new DeleteEntitiesCommand(new[] { line.Id });

        command.Execute(document);
        command.Undo(document);

        Assert.True(document.Entities.Contains(line.Id));
        var restoredDimension = Assert.IsType<LinearDimensionEntity>(
            document.Entities.GetRequired(dimension.Id));
        Assert.False(restoredDimension.IsStale);
    }

    [Fact]
    public void Execute_WhenDeletingOnlyDimension_ShouldNotMarkOtherDimensionsStale()
    {
        var document = new CadDocument();

        var deletedDimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, -2),
            DimensionOrientation.Horizontal);
        var remainingDimension = new LinearDimensionEntity(
            new Point2D(0, 5),
            new Point2D(10, 5),
            new Point2D(5, 7),
            DimensionOrientation.Horizontal);

        document.AddEntity(deletedDimension);
        document.AddEntity(remainingDimension);

        var command = new DeleteEntitiesCommand(new[] { deletedDimension.Id });

        command.Execute(document);

        Assert.False(document.Entities.Contains(deletedDimension.Id));
        var unchangedDimension = Assert.IsType<LinearDimensionEntity>(
            document.Entities.GetRequired(remainingDimension.Id));
        Assert.False(unchangedDimension.IsStale);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new DeleteEntitiesCommand(Array.Empty<OpenCad2D.Core.Identifiers.EntityId>()));
    }
}