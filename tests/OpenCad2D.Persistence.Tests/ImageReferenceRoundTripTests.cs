using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class ImageReferenceRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveExternalImageReference()
    {
        var document = new CadDocument();
        var image = new ImageReferenceEntity(
            @"C:\Temp\plan.png",
            new Point2D(10, 20),
            new Vector2D(30, 0),
            new Vector2D(0, 15),
            pixelWidth: 1200,
            pixelHeight: 600,
            id: new EntityId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        document.AddEntity(image);

        var serializer = new JsonDocumentSerializer();
        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        ImageReferenceEntity restoredImage = Assert.IsType<ImageReferenceEntity>(
            restored.Entities.GetRequired(new EntityId(Guid.Parse("11111111-1111-1111-1111-111111111111"))));

        Assert.Equal(@"C:\Temp\plan.png", restoredImage.FilePath);
        Assert.Equal(image.Origin, restoredImage.Origin);
        Assert.Equal(image.WidthVector, restoredImage.WidthVector);
        Assert.Equal(image.HeightVector, restoredImage.HeightVector);
        Assert.Equal(1200, restoredImage.PixelWidth);
        Assert.Equal(600, restoredImage.PixelHeight);
    }
}
