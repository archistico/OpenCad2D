using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Tests;

public sealed class DimensionStyleCollectionTests
{
    [Fact]
    public void Default_ShouldContainBuiltInPresetStyles()
    {
        DimensionStyleCollection styles = DimensionStyleCollection.Default;

        Assert.True(styles.Contains(DimensionStyleId.Standard));
        Assert.True(styles.Contains(DimensionStyleId.Architectural));
        Assert.True(styles.Contains(DimensionStyleId.Mechanical));

        DimensionStyle standard = styles.GetById(DimensionStyleId.Standard);
        Assert.Equal(TextFormatId.Annotation, standard.TextFormatId);
        Assert.Equal(4.0, standard.ArrowSize);
        Assert.Equal(2.0, standard.TextOffset);
        Assert.Equal(DimensionArrowSymbol.ClosedArrow, standard.ArrowSymbol);
        Assert.Equal(DimensionTextFitMode.OutsideWhenNeeded, standard.TextFitMode);
        Assert.Equal(DimensionTerminatorFitMode.OutsideWhenNeeded, standard.TerminatorFitMode);
        Assert.True(standard.IsBuiltIn);

        DimensionStyle architectural = styles.GetById(DimensionStyleId.Architectural);
        Assert.Equal(" m", architectural.Suffix);
        Assert.Equal(-2.0, architectural.TextOffset);
        Assert.Equal(DimensionArrowSymbol.ArchitecturalTick, architectural.ArrowSymbol);
        Assert.Equal(DimensionTextRotationMode.Readable, architectural.TextRotationMode);
        Assert.Equal(DimensionTextFitMode.OutsideWhenNeeded, architectural.TextFitMode);
        Assert.Equal(DimensionTerminatorFitMode.OutsideWhenNeeded, architectural.TerminatorFitMode);
        Assert.True(architectural.IsBuiltIn);

        DimensionStyle mechanical = styles.GetById(DimensionStyleId.Mechanical);
        Assert.Equal(" mm", mechanical.Suffix);
        Assert.Equal(3.0, mechanical.ArrowSize);
        Assert.Equal(DimensionArrowSymbol.ClosedFilledTriangle, mechanical.ArrowSymbol);
        Assert.Equal(DimensionTextRotationMode.Horizontal, mechanical.TextRotationMode);
        Assert.Equal(DimensionTextFitMode.OutsideWhenNeeded, mechanical.TextFitMode);
        Assert.Equal(DimensionTerminatorFitMode.OutsideWhenNeeded, mechanical.TerminatorFitMode);
        Assert.True(mechanical.IsBuiltIn);
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
