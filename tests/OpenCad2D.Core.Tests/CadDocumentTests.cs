using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentTests
{
    [Fact]
    public void Constructor_ShouldCreateDefaultLayerAndEmptyEntityCollection()
    {
        var document = new CadDocument();

        Assert.True(document.Layers.Contains(LayerId.Default));
        Assert.Equal(0, document.Entities.Count);
    }

    [Fact]
    public void AddEntity_WithDefaultLayer_ShouldAddEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void AddEntity_WithMissingLayer_ShouldThrow()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: new LayerId("Missing"));

        Assert.Throws<InvalidOperationException>(() =>
            document.AddEntity(line));
    }

    [Fact]
    public void AddEntity_WithExistingCustomLayer_ShouldAddEntity()
    {
        var document = new CadDocument();

        document.Layers.Add(new Layer(
            new LayerId("Walls"),
            "Walls"));

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: new LayerId("Walls"));

        document.AddEntity(line);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void ReplaceEntity_WithExistingEntity_ShouldReplaceEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var moved = new LineEntity(
            new Point2D(5, 0),
            new Point2D(15, 0),
            id: line.Id);

        document.ReplaceEntity(moved);

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 0), result.Start);
        Assert.Equal(new Point2D(15, 0), result.End);
    }

    [Fact]
    public void ReplaceEntity_WithMissingLayer_ShouldThrow()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var moved = new LineEntity(
            new Point2D(5, 0),
            new Point2D(15, 0),
            id: line.Id,
            layerId: new LayerId("Missing"));

        Assert.Throws<InvalidOperationException>(() =>
            document.ReplaceEntity(moved));
    }

    [Fact]
    public void RemoveEntity_ShouldRemoveEntity()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        document.RemoveEntity(line.Id);

        Assert.False(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void RemoveEntities_ShouldRemoveAllSpecifiedEntities()
    {
        var document = new CadDocument();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));

        document.AddEntity(first);
        document.AddEntity(second);

        document.RemoveEntities(new[] { first.Id, second.Id });

        Assert.False(document.Entities.Contains(first.Id));
        Assert.False(document.Entities.Contains(second.Id));
    }

    [Fact]
    public void ReplaceEntities_ShouldThrow_WhenEntityLayerDoesNotExist()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var invalidLayerLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(20, 0),
            id: line.Id,
            layerId: new LayerId("Missing"));

        Assert.Throws<InvalidOperationException>(() =>
            document.ReplaceEntities(new[] { invalidLayerLine }));
    }
}