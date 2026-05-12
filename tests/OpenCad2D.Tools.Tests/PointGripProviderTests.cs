using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Grips;

namespace OpenCad2D.Tools.Tests;

public sealed class PointGripProviderTests
{
    [Fact]
    public void GetGrips_ShouldReturnSingleMoveGripAtPointPosition()
    {
        var provider = new PointGripProvider();
        var point = new PointEntity(new Point2D(10, 20));

        GripPoint grip = Assert.Single(provider.GetGrips(point));

        Assert.Equal(point.Id, grip.EntityId);
        Assert.Equal(GripKind.MoveEntity, grip.Kind);
        Assert.Equal(0, grip.GripIndex);
        Assert.Equal(point.Position, grip.Position);
    }

    [Fact]
    public void ApplyGripMove_ShouldMovePointAndPreserveMetadata()
    {
        EntityId id = EntityId.New();
        LayerId layerId = new("Details");
        var provider = new PointGripProvider();
        var point = new PointEntity(
            new Point2D(0, 0),
            id,
            layerId,
            isVisible: false,
            isLocked: true,
            drawOrder: 4);

        var moved = Assert.IsType<PointEntity>(provider.ApplyGripMove(
            point,
            0,
            new Point2D(7, 8)));

        Assert.Equal(id, moved.Id);
        Assert.Equal(layerId, moved.LayerId);
        Assert.False(moved.IsVisible);
        Assert.True(moved.IsLocked);
        Assert.Equal(4, moved.DrawOrder);
        Assert.Equal(new Point2D(7, 8), moved.Position);
    }

    [Fact]
    public void DefaultRegistry_ShouldResolvePointProvider()
    {
        var registry = new GripProviderRegistry();
        var point = new PointEntity(new Point2D(1, 2));

        IGripProvider? provider = registry.FindProvider(point);

        Assert.IsType<PointGripProvider>(provider);
    }
}
