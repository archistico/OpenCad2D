using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class TextRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveTextFormatsAndTextEntities()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        var customFormatId = new TextFormatId("Note");

        document.ReplaceTextFormats(new TextFormatCollection(new[]
        {
            TextFormatCollection.Default.GetById(TextFormatId.Standard),
            new TextFormat(
                customFormatId,
                "Note",
                "Consolas",
                3.5,
                CadColor.FromRgb(10, 20, 30),
                isItalic: true)
        }));

        var text = new TextEntity(
            new Point2D(12, 34),
            "Hello",
            25,
            customFormatId);

        document.AddEntity(text);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        Assert.True(restored.TextFormats.Contains(customFormatId));
        TextEntity restoredText = Assert.IsType<TextEntity>(restored.Entities.GetRequired(text.Id));
        Assert.Equal("Hello", restoredText.Text);
        Assert.Equal(new Point2D(12, 34), restoredText.InsertionPoint);
        Assert.Equal(25, restoredText.RotationDegrees);
        Assert.Equal(customFormatId, restoredText.TextFormatId);
    }
}
