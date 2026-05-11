using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class UpdateLineFormatsCommandTests
{
    [Fact]
    public void Execute_ShouldReplaceLineFormats()
    {
        var document = new CadDocument();

        var nextFormats = new LineFormatCollection(new[]
        {
            CreateFormat(LineFormatId.Continuous, "Continuous custom"),
            CreateFormat(new LineFormatId("Custom"), "Custom"),
        });

        var command = new UpdateLineFormatsCommand(
            document.LineFormats,
            nextFormats);

        command.Execute(document);

        Assert.True(document.LineFormats.Contains(new LineFormatId("Custom")));
        Assert.Equal("Continuous custom", document.LineFormats.GetById(LineFormatId.Continuous).Name);
    }

    [Fact]
    public void Execute_ShouldRebaseLayersWithRemovedFormatToContinuous()
    {
        var document = new CadDocument();
        var customFormatId = new LineFormatId("Custom");

        document.LineFormats.ReplaceAll(new[]
        {
            CreateFormat(LineFormatId.Continuous, "Continuous"),
            CreateFormat(customFormatId, "Custom"),
        });

        document.Layers.Add(new Layer(
            new LayerId("Walls"),
            "Walls",
            customFormatId));

        var nextFormats = new LineFormatCollection(new[]
        {
            CreateFormat(LineFormatId.Continuous, "Continuous"),
        });

        var command = new UpdateLineFormatsCommand(
            document.LineFormats,
            nextFormats);

        command.Execute(document);

        Layer layer = document.Layers.GetRequired(new LayerId("Walls"));
        Assert.Equal(LineFormatId.Continuous, layer.LineFormatId);
    }

    [Fact]
    public void Undo_ShouldRestorePreviousFormatsAndLayerReferences()
    {
        var document = new CadDocument();
        var customFormatId = new LineFormatId("Custom");

        document.LineFormats.ReplaceAll(new[]
        {
            CreateFormat(LineFormatId.Continuous, "Continuous"),
            CreateFormat(customFormatId, "Custom"),
        });

        document.Layers.Add(new Layer(
            new LayerId("Walls"),
            "Walls",
            customFormatId));

        var nextFormats = new LineFormatCollection(new[]
        {
            CreateFormat(LineFormatId.Continuous, "Continuous changed"),
        });

        var command = new UpdateLineFormatsCommand(
            document.LineFormats,
            nextFormats);

        command.Execute(document);
        command.Undo(document);

        Assert.True(document.LineFormats.Contains(customFormatId));
        Layer layer = document.Layers.GetRequired(new LayerId("Walls"));
        Assert.Equal(customFormatId, layer.LineFormatId);
    }

    [Fact]
    public void Execute_WithoutContinuousFormat_ShouldThrow()
    {
        var document = new CadDocument();

        var nextFormats = new LineFormatCollection(new[]
        {
            CreateFormat(new LineFormatId("OnlyCustom"), "Only custom"),
        });

        var command = new UpdateLineFormatsCommand(
            document.LineFormats,
            nextFormats);

        Assert.Throws<InvalidOperationException>(() =>
            command.Execute(document));
    }

    private static LineFormat CreateFormat(
        LineFormatId id,
        string name)
    {
        return new LineFormat(
            id,
            name,
            CadColor.FromRgb(255, 255, 255),
            LineWeight.FromMillimeters(0.25),
            LineStyle.Continuous);
    }
}
