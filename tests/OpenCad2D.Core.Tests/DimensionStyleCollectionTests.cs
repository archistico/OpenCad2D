using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Tests;

public sealed class DimensionStyleCollectionTests
{
    [Fact]
    public void Default_ShouldContainStandardStyle()
    {
        DimensionStyleCollection styles = DimensionStyleCollection.Default;

        Assert.True(styles.Contains(DimensionStyleId.Standard));

        DimensionStyle standard = styles.GetById(DimensionStyleId.Standard);
        Assert.Equal(TextFormatId.Annotation, standard.TextFormatId);
        Assert.Equal(4.0, standard.ArrowSize);
        Assert.Equal(2.0, standard.TextOffset);
    }

    [Fact]
    public void Constructor_WithDuplicateIds_ShouldThrow()
    {
        var first = CreateStyle(
            DimensionStyleId.Standard,
            "Standard");

        var second = CreateStyle(
            DimensionStyleId.Standard,
            "Standard Copy");

        Assert.Throws<InvalidOperationException>(() =>
            new DimensionStyleCollection(new[] { first, second }));
    }

    [Fact]
    public void Constructor_WithDuplicateNames_ShouldThrow()
    {
        var first = CreateStyle(
            new DimensionStyleId("A"),
            "Same");

        var second = CreateStyle(
            new DimensionStyleId("B"),
            "same");

        Assert.Throws<InvalidOperationException>(() =>
            new DimensionStyleCollection(new[] { first, second }));
    }

    private static DimensionStyle CreateStyle(
        DimensionStyleId id,
        string name)
    {
        return new DimensionStyle(
            id,
            name,
            TextFormatId.Annotation,
            arrowSize: 4,
            textOffset: 2,
            extensionLineOffset: 1.5,
            extensionLineOvershoot: 2,
            decimalPlaces: 2);
    }
}
