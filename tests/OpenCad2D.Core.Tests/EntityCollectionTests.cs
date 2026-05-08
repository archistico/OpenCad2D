using OpenCad2D.Core.Collections;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class EntityCollectionTests
{
    [Fact]
    public void Add_ShouldAddEntity()
    {
        var entities = new EntityCollection();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        entities.Add(line);

        Assert.Equal(1, entities.Count);
        Assert.True(entities.Contains(line.Id));
    }

    [Fact]
    public void Add_WithDuplicateId_ShouldThrow()
    {
        var entities = new EntityCollection();
        EntityId id = EntityId.New();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            id);

        var second = new LineEntity(
            new Point2D(0, 0),
            new Point2D(20, 0),
            id);

        entities.Add(first);

        Assert.Throws<InvalidOperationException>(() =>
            entities.Add(second));
    }

    [Fact]
    public void GetRequired_WithExistingEntity_ShouldReturnEntity()
    {
        var entities = new EntityCollection();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        entities.Add(line);

        CadEntity result = entities.GetRequired(line.Id);

        Assert.Equal(line.Id, result.Id);
    }

    [Fact]
    public void GetRequired_WithMissingEntity_ShouldThrow()
    {
        var entities = new EntityCollection();

        Assert.Throws<KeyNotFoundException>(() =>
            entities.GetRequired(EntityId.New()));
    }

    [Fact]
    public void RemoveRequired_WithExistingEntity_ShouldRemoveEntity()
    {
        var entities = new EntityCollection();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        entities.Add(line);

        entities.RemoveRequired(line.Id);

        Assert.Equal(0, entities.Count);
        Assert.False(entities.Contains(line.Id));
    }

    [Fact]
    public void RemoveRequired_WithMissingEntity_ShouldThrow()
    {
        var entities = new EntityCollection();

        Assert.Throws<KeyNotFoundException>(() =>
            entities.RemoveRequired(EntityId.New()));
    }

    [Fact]
    public void Replace_WithExistingEntity_ShouldReplaceEntity()
    {
        var entities = new EntityCollection();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        entities.Add(line);

        var moved = (LineEntity)line.Transform(
            Matrix2D.Translation(5, 0));

        entities.Replace(moved);

        var result = (LineEntity)entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 0), result.Start);
        Assert.Equal(new Point2D(15, 0), result.End);
    }

    [Fact]
    public void Replace_WithMissingEntity_ShouldThrow()
    {
        var entities = new EntityCollection();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        Assert.Throws<KeyNotFoundException>(() =>
            entities.Replace(line));
    }

    [Fact]
    public void GetByIds_ShouldReturnEntitiesInRequestedOrder()
    {
        var entities = new EntityCollection();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new CircleEntity(
            new Point2D(0, 0),
            5);

        entities.Add(first);
        entities.Add(second);

        IReadOnlyList<CadEntity> result = entities.GetByIds(
            new[] { second.Id, first.Id });

        Assert.Equal(second.Id, result[0].Id);
        Assert.Equal(first.Id, result[1].Id);
    }
}