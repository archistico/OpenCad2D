using OpenCad2D.Core.Collections;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Spatial;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class SpatialIndexTests
{
    [Fact]
    public void Query_ShouldReturnEntitiesWhoseBoundsIntersectArea()
    {
        var collection = new EntityCollection(
            new LinearSpatialIndex());

        var inside = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var outside = new LineEntity(
            new Point2D(100, 100),
            new Point2D(110, 100));

        collection.Add(inside);
        collection.Add(outside);

        IReadOnlyList<CadEntity> result = collection.Query(
            new BoundingBox2D(-1, -1, 11, 1));

        Assert.Single(result);
        Assert.Equal(inside.Id, result[0].Id);
    }

    [Fact]
    public void Query_ShouldNotReturnRemovedEntity()
    {
        var collection = new EntityCollection(
            new LinearSpatialIndex());

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        collection.Add(line);
        collection.RemoveRequired(line.Id);

        IReadOnlyList<CadEntity> result = collection.Query(
            new BoundingBox2D(-1, -1, 11, 1));

        Assert.Empty(result);
    }

    [Fact]
    public void Query_ShouldUseUpdatedBoundsAfterReplace()
    {
        var collection = new EntityCollection(
            new LinearSpatialIndex());

        var original = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        collection.Add(original);

        CadEntity moved = original.Transform(
            OpenCad2D.Geometry.Transformations.Matrix2D.Translation(
                100,
                0));

        collection.Replace(moved);

        IReadOnlyList<CadEntity> oldAreaResult = collection.Query(
            new BoundingBox2D(-1, -1, 11, 1));

        IReadOnlyList<CadEntity> newAreaResult = collection.Query(
            new BoundingBox2D(99, -1, 111, 1));

        Assert.Empty(oldAreaResult);
        Assert.Single(newAreaResult);
        Assert.Equal(original.Id, newAreaResult[0].Id);
    }

    [Fact]
    public void Clear_ShouldClearSpatialIndex()
    {
        var collection = new EntityCollection(
            new LinearSpatialIndex());

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        collection.Add(line);
        collection.Clear();

        IReadOnlyList<CadEntity> result = collection.Query(
            new BoundingBox2D(-1, -1, 11, 1));

        Assert.Empty(result);
    }
}