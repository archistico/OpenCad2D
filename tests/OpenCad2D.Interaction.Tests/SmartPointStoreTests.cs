using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class SmartPointStoreTests
{
    [Fact]
    public void Constructor_ShouldRejectNonPositiveMaximumCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SmartPointStore(0));
    }

    [Fact]
    public void AddOrRefresh_ShouldKeepAtMostMaximumCount()
    {
        var store = new SmartPointStore(maximumCount: 2);

        store.AddOrRefresh(CreatePoint(0));
        store.AddOrRefresh(CreatePoint(1));
        store.AddOrRefresh(CreatePoint(2));

        Assert.Equal(2, store.Points.Count);
        Assert.Equal(new Point2D(1, 1), store.Points[0].Position);
        Assert.Equal(new Point2D(2, 2), store.Points[1].Position);
    }

    [Fact]
    public void AddOrRefresh_ShouldMoveExistingReferenceToNewestPosition()
    {
        EntityId entityId = EntityId.New();
        var store = new SmartPointStore(maximumCount: 3);
        var first = new SmartPoint(
            new Point2D(10, 20),
            SnapKind.Endpoint,
            entityId,
            DateTimeOffset.UtcNow);
        var second = CreatePoint(1);
        var refreshed = new SmartPoint(
            new Point2D(10, 20),
            SnapKind.Endpoint,
            entityId,
            DateTimeOffset.UtcNow.AddSeconds(1));

        store.AddOrRefresh(first);
        store.AddOrRefresh(second);
        store.AddOrRefresh(refreshed);

        Assert.Equal(2, store.Points.Count);
        Assert.Equal(new Point2D(1, 1), store.Points[0].Position);
        Assert.Equal(new Point2D(10, 20), store.Points[1].Position);
        Assert.Same(refreshed, store.Points[1]);
    }

    [Fact]
    public void Clear_ShouldRemoveAllSmartPoints()
    {
        var store = new SmartPointStore(maximumCount: 2);

        store.AddOrRefresh(CreatePoint(0));
        store.AddOrRefresh(CreatePoint(1));
        store.Clear();

        Assert.Empty(store.Points);
    }

    private static SmartPoint CreatePoint(int index)
    {
        return new SmartPoint(
            new Point2D(index, index),
            SnapKind.Endpoint,
            EntityId.New(),
            DateTimeOffset.UtcNow.AddSeconds(index));
    }
}
