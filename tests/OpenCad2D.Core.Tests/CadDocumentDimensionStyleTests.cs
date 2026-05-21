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
        Assert.True(document.DimensionStyles.Contains(DimensionStyleId.Architectural));
        Assert.True(document.DimensionStyles.Contains(DimensionStyleId.Mechanical));
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

public sealed class CadDocumentCurrentDimensionStyleTests
{
    [Fact]
    public void Constructor_ShouldUseStandardAsCurrentDimensionStyle()
    {
        var document = new CadDocument();

        Assert.Equal(DimensionStyleId.Standard, document.CurrentDimensionStyleId);
    }

    [Fact]
    public void SetCurrentDimensionStyle_ShouldUpdateCurrentStyle()
    {
        var document = new CadDocument();
        var customStyleId = new DimensionStyleId("Custom");
        document.ReplaceDimensionStyles(new DimensionStyleCollection(new[]
        {
            DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard),
            new DimensionStyle(
                customStyleId,
                "Custom",
                TextFormatId.Annotation,
                arrowSize: 5,
                textOffset: 3,
                extensionLineOffset: 1,
                extensionLineOvershoot: 2,
                decimalPlaces: 1)
        }));

        document.SetCurrentDimensionStyle(customStyleId);

        Assert.Equal(customStyleId, document.CurrentDimensionStyleId);
    }
}
