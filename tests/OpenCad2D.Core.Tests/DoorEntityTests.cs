using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Architecture.Doors;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class DoorEntityTests
{
    [Fact]
    public void Constructor_ShouldStoreParametricDoorValues()
    {
        var id = new EntityId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var layerId = new LayerId("Walls");

        var door = new DoorEntity(
            new Point2D(10, 20),
            width: 90,
            wallThickness: 20,
            openingAngleDegrees: 90,
            swingDirection: DoorSwingDirection.Right,
            anchor: AnchorPoint.Center,
            id: id,
            layerId: layerId);

        Assert.Equal(EntityKind.Door, door.Kind);
        Assert.Equal(id, door.Id);
        Assert.Equal(layerId, door.LayerId);
        Assert.Equal(new Point2D(10, 20), door.InsertionPoint);
        Assert.Equal(90, door.Width);
        Assert.Equal(20, door.WallThickness);
        Assert.Equal(90, door.OpeningAngleDegrees);
        Assert.Equal(DoorSwingDirection.Right, door.SwingDirection);
        Assert.Equal(AnchorPoint.Center, door.Anchor);
        Assert.True(door.MaskWallOpening);
    }

    [Fact]
    public void GetGeneratedGeometry_ShouldPlaceMiddleLeftAnchorAtInsertionPoint()
    {
        var door = new DoorEntity(
            new Point2D(10, 20),
            width: 90,
            wallThickness: 20,
            anchor: AnchorPoint.MiddleLeft);

        Assert.Contains(
            door.GetGeneratedGeometry().Segments,
            segment => segment.Start == new Point2D(10, 10) &&
                       segment.End == new Point2D(100, 10));

        Assert.Contains(
            door.GetGeneratedGeometry().Segments,
            segment => segment.Start == new Point2D(10, 20) &&
                       Math.Abs(segment.End.X - 10) < 1e-9 &&
                       Math.Abs(segment.End.Y - 110) < 1e-9);
    }

    [Fact]
    public void GetGeneratedGeometry_WithRightSwing_ShouldDrawLeafOnNegativeSide()
    {
        var door = new DoorEntity(
            Point2D.Origin,
            width: 90,
            wallThickness: 20,
            swingDirection: DoorSwingDirection.Right,
            anchor: AnchorPoint.MiddleLeft);

        Assert.Contains(
            door.GetGeneratedGeometry().Segments,
            segment => segment.Start == Point2D.Origin &&
                       Math.Abs(segment.End.X) < 1e-9 &&
                       Math.Abs(segment.End.Y + 90) < 1e-9);
    }


    [Fact]
    public void GetGeneratedGeometry_WithMaskEnabled_ShouldExposeWallMaskPolygon()
    {
        var door = new DoorEntity(
            new Point2D(10, 20),
            width: 90,
            wallThickness: 20,
            anchor: AnchorPoint.MiddleLeft,
            maskWallOpening: true);

        DoorGeometry geometry = door.GetGeneratedGeometry();

        Assert.True(geometry.HasWallMask);
        Assert.Equal(4, geometry.WallMaskPolygon.Count);
        Assert.Contains(new Point2D(10, 10), geometry.WallMaskPolygon);
        Assert.Contains(new Point2D(100, 30), geometry.WallMaskPolygon);
    }

    [Fact]
    public void GetGeneratedGeometry_WithMaskDisabled_ShouldNotExposeWallMaskPolygon()
    {
        var door = new DoorEntity(
            Point2D.Origin,
            width: 90,
            wallThickness: 20,
            maskWallOpening: false);

        DoorGeometry geometry = door.GetGeneratedGeometry();

        Assert.False(geometry.HasWallMask);
        Assert.Empty(geometry.WallMaskPolygon);
    }

    [Fact]
    public void Transform_ShouldMoveInsertionPointAndScaleParameters()
    {
        var door = new DoorEntity(
            new Point2D(10, 20),
            width: 90,
            wallThickness: 20);

        DoorEntity transformed = Assert.IsType<DoorEntity>(
            door.Transform(Matrix2D.Scale(2.0, Point2D.Origin)));

        Assert.Equal(new Point2D(20, 40), transformed.InsertionPoint);
        Assert.Equal(180, transformed.Width);
        Assert.Equal(40, transformed.WallThickness);
        Assert.Equal(door.OpeningAngleDegrees, transformed.OpeningAngleDegrees);
        Assert.Equal(door.SwingDirection, transformed.SwingDirection);
        Assert.Equal(door.Anchor, transformed.Anchor);
        Assert.Equal(door.MaskWallOpening, transformed.MaskWallOpening);
    }

    [Theory]
    [InlineData(0, 20, 90)]
    [InlineData(90, 0, 90)]
    [InlineData(90, 20, 0)]
    [InlineData(90, 20, 181)]
    public void Constructor_WithInvalidParameters_ShouldThrow(
        double width,
        double wallThickness,
        double openingAngle)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DoorEntity(
            Point2D.Origin,
            width,
            wallThickness,
            openingAngle));
    }
}
