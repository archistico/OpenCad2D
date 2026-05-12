using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class UpdateTextFormatsCommandTests
{
    [Fact]
    public void Execute_ShouldReplaceTextFormats()
    {
        var document = new CadDocument();

        var nextFormats = new TextFormatCollection(new[]
        {
            CreateFormat(TextFormatId.Standard, "Standard custom"),
            CreateFormat(new TextFormatId("Custom"), "Custom"),
        });

        var command = new UpdateTextFormatsCommand(
            document.TextFormats,
            nextFormats);

        command.Execute(document);

        Assert.True(document.TextFormats.Contains(new TextFormatId("Custom")));
        Assert.Equal("Standard custom", document.TextFormats.GetById(TextFormatId.Standard).Name);
    }

    [Fact]
    public void Execute_ShouldRebaseTextEntitiesWithRemovedFormatToStandard()
    {
        var document = new CadDocument();
        var customFormatId = new TextFormatId("Custom");

        document.TextFormats.ReplaceAll(new[]
        {
            CreateFormat(TextFormatId.Standard, "Standard"),
            CreateFormat(customFormatId, "Custom"),
        });

        var text = new TextEntity(
            new Point2D(10, 20),
            "Room",
            textFormatId: customFormatId);

        document.AddEntity(text);

        var nextFormats = new TextFormatCollection(new[]
        {
            CreateFormat(TextFormatId.Standard, "Standard"),
        });

        var command = new UpdateTextFormatsCommand(
            document.TextFormats,
            nextFormats);

        command.Execute(document);

        var updatedText = Assert.IsType<TextEntity>(document.Entities.GetRequired(text.Id));
        Assert.Equal(TextFormatId.Standard, updatedText.TextFormatId);
    }

    [Fact]
    public void Undo_ShouldRestorePreviousFormatsAndTextEntityReferences()
    {
        var document = new CadDocument();
        var customFormatId = new TextFormatId("Custom");

        document.TextFormats.ReplaceAll(new[]
        {
            CreateFormat(TextFormatId.Standard, "Standard"),
            CreateFormat(customFormatId, "Custom"),
        });

        var text = new TextEntity(
            new Point2D(10, 20),
            "Room",
            textFormatId: customFormatId);

        document.AddEntity(text);

        var nextFormats = new TextFormatCollection(new[]
        {
            CreateFormat(TextFormatId.Standard, "Standard changed"),
        });

        var command = new UpdateTextFormatsCommand(
            document.TextFormats,
            nextFormats);

        command.Execute(document);
        command.Undo(document);

        Assert.True(document.TextFormats.Contains(customFormatId));
        var restoredText = Assert.IsType<TextEntity>(document.Entities.GetRequired(text.Id));
        Assert.Equal(customFormatId, restoredText.TextFormatId);
    }

    [Fact]
    public void Execute_WithoutStandardFormat_ShouldThrow()
    {
        var document = new CadDocument();

        var nextFormats = new TextFormatCollection(new[]
        {
            CreateFormat(new TextFormatId("OnlyCustom"), "Only custom"),
        });

        var command = new UpdateTextFormatsCommand(
            document.TextFormats,
            nextFormats);

        Assert.Throws<InvalidOperationException>(() =>
            command.Execute(document));
    }

    private static TextFormat CreateFormat(
        TextFormatId id,
        string name)
    {
        return new TextFormat(
            id,
            name,
            "Arial",
            10,
            CadColor.FromRgb(255, 255, 255));
    }
}
