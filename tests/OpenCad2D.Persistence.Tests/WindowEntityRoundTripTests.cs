using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class WindowEntityRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveParametricWindowEntity()
    {
        var document = new CadDocument();
        var window = new WindowEntity(
            new Point2D(10, 20),
            width: 120,
            wallThickness: 30,
            frameOffset: 5,
            anchor: AnchorPoint.TopCenter,
            maskWallOpening: false);

        document.AddEntity(window);

        var serializer = new JsonDocumentSerializer();
        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        WindowEntityDto windowDto = Assert.IsType<WindowEntityDto>(Assert.Single(dto.Entities));
        Assert.Equal(EntityTypeNames.Window, windowDto.Type);
        Assert.Equal(120, windowDto.Width);
        Assert.Equal(30, windowDto.WallThickness);
        Assert.Equal(5, windowDto.FrameOffset);
        Assert.Equal("TopCenter", windowDto.Anchor);
        Assert.False(windowDto.MaskWallOpening);

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);
        WindowEntity restoredWindow = Assert.IsType<WindowEntity>(
            Assert.Single(restored.Entities.All));

        Assert.Equal(window.InsertionPoint, restoredWindow.InsertionPoint);
        Assert.Equal(window.Width, restoredWindow.Width);
        Assert.Equal(window.WallThickness, restoredWindow.WallThickness);
        Assert.Equal(window.FrameOffset, restoredWindow.FrameOffset);
        Assert.Equal(window.Anchor, restoredWindow.Anchor);
        Assert.Equal(window.MaskWallOpening, restoredWindow.MaskWallOpening);
    }

    [Fact]
    public void Deserialize_WithInvalidWindow_ShouldSkipEntity()
    {
        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = LayerId.Default.Value
            },
            Viewport = new ViewportStateDto(),
            Entities = new List<EntityDto>
            {
                new WindowEntityDto
                {
                    Id = Guid.NewGuid().ToString(),
                    LayerId = LayerId.Default.Value,
                    Width = 120,
                    WallThickness = 20,
                    FrameOffset = 99
                }
            }
        };

        var serializer = new JsonDocumentSerializer();

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        Assert.Empty(restored.Entities.All);
    }
}
