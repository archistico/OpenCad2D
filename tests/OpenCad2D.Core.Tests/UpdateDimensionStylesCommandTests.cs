using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Tests;

public sealed class UpdateDimensionStylesCommandTests
{
    [Fact]
    public void Execute_ShouldReplaceDimensionStylesAndCurrentStyle()
    {
        var document = new CadDocument();
        var customId = new DimensionStyleId("Custom");

        var nextStyles = new[]
        {
            CreateStyle(DimensionStyleId.Standard, "Standard custom"),
            CreateStyle(customId, "Custom")
        };

        var command = new UpdateDimensionStylesCommand(
            document.DimensionStyles.All,
            nextStyles,
            document.CurrentDimensionStyleId,
            customId);

        command.Execute(document);

        Assert.True(document.DimensionStyles.Contains(customId));
        Assert.Equal(customId, document.CurrentDimensionStyleId);
        Assert.Equal("Standard custom", document.DimensionStyles.GetById(DimensionStyleId.Standard).Name);
    }

    [Fact]
    public void Undo_ShouldRestorePreviousDimensionStylesAndCurrentStyle()
    {
        var document = new CadDocument();
        var customId = new DimensionStyleId("Custom");

        var nextStyles = new[]
        {
            CreateStyle(DimensionStyleId.Standard, "Standard custom"),
            CreateStyle(customId, "Custom")
        };

        var command = new UpdateDimensionStylesCommand(
            document.DimensionStyles.All,
            nextStyles,
            document.CurrentDimensionStyleId,
            customId);

        command.Execute(document);
        command.Undo(document);

        Assert.False(document.DimensionStyles.Contains(customId));
        Assert.Equal(DimensionStyleId.Standard, document.CurrentDimensionStyleId);
        Assert.Equal("Standard", document.DimensionStyles.GetById(DimensionStyleId.Standard).Name);
    }

    [Fact]
    public void Execute_WithoutStandardStyle_ShouldThrow()
    {
        var document = new CadDocument();
        var customId = new DimensionStyleId("Custom");

        var command = new UpdateDimensionStylesCommand(
            document.DimensionStyles.All,
            new[] { CreateStyle(customId, "Custom") },
            document.CurrentDimensionStyleId,
            customId);

        Assert.Throws<InvalidOperationException>(() =>
            command.Execute(document));
    }

    private static DimensionStyle CreateStyle(
        DimensionStyleId id,
        string name)
    {
        return new DimensionStyle(
            id,
            name,
            TextFormatId.Annotation,
            arrowSize: 4.0,
            textOffset: 2.0,
            extensionLineOffset: 1.5,
            extensionLineOvershoot: 2.0,
            decimalPlaces: 2);
    }
}
