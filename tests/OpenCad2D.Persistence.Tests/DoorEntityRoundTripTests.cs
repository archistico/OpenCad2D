using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Architecture.Doors;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class DoorEntityRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveParametricDoorEntity()
    {
        var document = new CadDocument();
        var id = new EntityId(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var door = new DoorEntity(
            new Point2D(10, 20),
            width: 85,
            wallThickness: 30,
            openingAngleDegrees: 75,
            swingDirection: DoorSwingDirection.Right,
            anchor: AnchorPoint.BottomCenter,
            maskWallOpening: false,
            xAxis: new Vector2D(0, 1),
            yAxis: new Vector2D(-1, 0),
            id: id);
        document.AddEntity(door);

        var serializer = new JsonDocumentSerializer();
        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        DoorEntityDto doorDto = Assert.IsType<DoorEntityDto>(Assert.Single(dto.Entities));
        Assert.Equal("Door", doorDto.Type);
        Assert.Equal("Right", doorDto.SwingDirection);
        Assert.Equal("BottomCenter", doorDto.Anchor);
        Assert.False(doorDto.MaskWallOpening);

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        DoorEntity restoredDoor = Assert.IsType<DoorEntity>(
            restored.Entities.GetRequired(id));

        Assert.Equal(door.InsertionPoint, restoredDoor.InsertionPoint);
        Assert.Equal(85, restoredDoor.Width, precision: 6);
        Assert.Equal(30, restoredDoor.WallThickness, precision: 6);
        Assert.Equal(75, restoredDoor.OpeningAngleDegrees, precision: 6);
        Assert.Equal(DoorSwingDirection.Right, restoredDoor.SwingDirection);
        Assert.Equal(AnchorPoint.BottomCenter, restoredDoor.Anchor);
        Assert.False(restoredDoor.MaskWallOpening);
        Assert.Equal(door.XAxis, restoredDoor.XAxis);
        Assert.Equal(door.YAxis, restoredDoor.YAxis);
    }

    [Fact]
    public void Deserialize_WithInvalidAnchorAndSwing_ShouldUseSafeDefaults()
    {
        var serializer = new JsonDocumentSerializer();
        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = LayerId.Default.Value
            },
            Viewport = new ViewportStateDto(),
            Entities =
            {
                new DoorEntityDto
                {
                    Id = "55555555-5555-5555-5555-555555555555",
                    LayerId = LayerId.Default.Value,
                    Width = 90,
                    WallThickness = 20,
                    OpeningAngleDegrees = 90,
                    SwingDirection = "Invalid",
                    Anchor = "Invalid"
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        DoorEntity door = Assert.IsType<DoorEntity>(
            document.Entities.GetRequired(new EntityId(Guid.Parse("55555555-5555-5555-5555-555555555555"))));

        Assert.Equal(DoorSwingDirection.Left, door.SwingDirection);
        Assert.Equal(AnchorPoint.MiddleLeft, door.Anchor);
        Assert.True(door.MaskWallOpening);
    }
}
