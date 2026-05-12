using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class TextFormatTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var format = new TextFormat(
            TextFormatId.Standard,
            "Standard",
            "Arial",
            2.5,
            CadColor.FromRgb(255, 255, 255));

        Assert.Equal(TextFormatId.Standard, format.Id);
        Assert.Equal("Standard", format.Name);
        Assert.Equal("Arial", format.FontFamily);
        Assert.Equal(2.5, format.Height);
        Assert.True(format.IsBuiltIn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidHeight_ShouldThrow(double height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextFormat(
            TextFormatId.Standard,
            "Standard",
            "Arial",
            height,
            CadColor.FromRgb(255, 255, 255)));
    }
}
