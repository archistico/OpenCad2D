using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Tests;

public sealed class DimensionStyleTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var style = new DimensionStyle(
            new DimensionStyleId("Architectural"),
            "Architectural",
            TextFormatId.Annotation,
            arrowSize: 4,
            textOffset: 2,
            extensionLineOffset: 1.5,
            extensionLineOvershoot: 2,
            decimalPlaces: 2,
            decimalSeparator: ",",
            suffix: " mm");

        Assert.Equal(new DimensionStyleId("Architectural"), style.Id);
        Assert.Equal("Architectural", style.Name);
        Assert.Equal(TextFormatId.Annotation, style.TextFormatId);
        Assert.Equal(4, style.ArrowSize);
        Assert.Equal(2, style.TextOffset);
        Assert.Equal(1.5, style.ExtensionLineOffset);
        Assert.Equal(2, style.ExtensionLineOvershoot);
        Assert.Equal(2, style.DecimalPlaces);
        Assert.Equal(",", style.DecimalSeparator);
        Assert.Equal(" mm", style.Suffix);
    }

    [Fact]
    public void Constructor_WithNegativeDecimalPlaces_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DimensionStyle(
                DimensionStyleId.Standard,
                "Standard",
                TextFormatId.Annotation,
                arrowSize: 4,
                textOffset: 2,
                extensionLineOffset: 1.5,
                extensionLineOvershoot: 2,
                decimalPlaces: -1));
    }

    [Fact]
    public void Constructor_WithInvalidSeparator_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new DimensionStyle(
                DimensionStyleId.Standard,
                "Standard",
                TextFormatId.Annotation,
                arrowSize: 4,
                textOffset: 2,
                extensionLineOffset: 1.5,
                extensionLineOvershoot: 2,
                decimalPlaces: 2,
                decimalSeparator: ":"));
    }
}
