using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class TextFormatCollectionTests
{
    [Fact]
    public void Default_ShouldContainStandardFormat()
    {
        TextFormatCollection collection = TextFormatCollection.Default;

        Assert.True(collection.Contains(TextFormatId.Standard));
        Assert.NotNull(collection.GetById(TextFormatId.Standard));
    }


    [Fact]
    public void Default_ShouldUseReadableDrawingUnitHeights()
    {
        TextFormatCollection collection = TextFormatCollection.Default;

        Assert.Equal(10.0, collection.GetById(TextFormatId.Standard).Height);
        Assert.Equal(18.0, collection.GetById(TextFormatId.Title).Height);
        Assert.Equal(8.0, collection.GetById(TextFormatId.Annotation).Height);
        Assert.Equal(6.0, collection.GetById(TextFormatId.Small).Height);
    }

    [Fact]
    public void Constructor_WithDuplicateIds_ShouldThrow()
    {
        var first = new TextFormat(TextFormatId.Standard, "One", "Arial", 2.5, CadColor.FromRgb(255, 255, 255));
        var second = new TextFormat(TextFormatId.Standard, "Two", "Arial", 2.5, CadColor.FromRgb(255, 255, 255));

        Assert.Throws<InvalidOperationException>(() => new TextFormatCollection(new[] { first, second }));
    }
}
