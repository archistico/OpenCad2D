using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentDimensionStyleTests
{
    [Fact]
    public void Constructor_ShouldCreateDefaultDimensionStyles()
    {
        var document = new CadDocument();

        Assert.True(document.DimensionStyles.Contains(DimensionStyleId.Standard));
    }

    [Fact]
    public void ReplaceDimensionStyles_ShouldReplaceDocumentStyles()
    {
        var document = new CadDocument();
        var customStyleId = new DimensionStyleId("Custom");

        var styles = new DimensionStyleCollection(new[]
        {
            new DimensionStyle(
                customStyleId,
                "Custom",
                TextFormatId.Annotation,
                arrowSize: 5,
                textOffset: 3,
                extensionLineOffset: 1,
                extensionLineOvershoot: 2,
                decimalPlaces: 1)
        });

        document.ReplaceDimensionStyles(styles);

        Assert.True(document.DimensionStyles.Contains(customStyleId));
        Assert.False(document.DimensionStyles.Contains(DimensionStyleId.Standard));
    }
}
