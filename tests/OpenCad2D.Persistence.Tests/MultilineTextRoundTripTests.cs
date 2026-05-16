using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class MultilineTextRoundTripTests
{
    [Fact]
    public void SaveAndLoad_WhenDocumentContainsMultilineText_ShouldPreserveEntity()
    {
        var document = new CadDocument();
        var entity = new MultilineTextEntity(
            new Point2D(12, 34),
            "First line\nSecond line",
            25,
            TextFormatId.Annotation);
        document.AddEntity(entity);

        var serializer = new JsonDocumentSerializer();

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out string currentLayerId,
            out ViewportStateDto viewport);

        Assert.Equal(LayerId.Default.Value, currentLayerId);
        Assert.NotNull(viewport);

        MultilineTextEntity restoredText = Assert.IsType<MultilineTextEntity>(
            restored.Entities.GetRequired(entity.Id));
        Assert.Equal(entity.InsertionPoint, restoredText.InsertionPoint);
        Assert.Equal(entity.Text, restoredText.Text);
        Assert.Equal(entity.RotationDegrees, restoredText.RotationDegrees);
        Assert.Equal(entity.TextFormatId, restoredText.TextFormatId);
    }
}
