using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Grips;

namespace OpenCad2D.Tools.Tests;

public sealed class TextGripProviderTests
{
    [Fact]
    public void GetGrips_ShouldReturnInsertionPointGrip()
    {
        var entity = new TextEntity(new Point2D(1, 2), "Label");
        var provider = new TextGripProvider();

        GripPoint grip = Assert.Single(provider.GetGrips(entity));

        Assert.Equal(entity.InsertionPoint, grip.Position);
        Assert.Equal(GripKind.MoveEntity, grip.Kind);
        Assert.Equal(entity.Id, grip.EntityId);
    }

    [Fact]
    public void ApplyGripMove_ShouldMoveInsertionPointAndPreserveTextData()
    {
        var entity = new TextEntity(new Point2D(1, 2), "Label", 15, TextFormatId.Small);
        var provider = new TextGripProvider();

        var moved = Assert.IsType<TextEntity>(provider.ApplyGripMove(entity, 0, new Point2D(5, 6)));

        Assert.Equal(new Point2D(5, 6), moved.InsertionPoint);
        Assert.Equal(entity.Text, moved.Text);
        Assert.Equal(entity.RotationDegrees, moved.RotationDegrees);
        Assert.Equal(entity.TextFormatId, moved.TextFormatId);
    }
}
