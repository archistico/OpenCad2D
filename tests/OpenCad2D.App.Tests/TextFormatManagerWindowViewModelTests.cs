using System.Linq;
using Avalonia.Media;
using OpenCad2D.App.ViewModels.TextFormats;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class TextFormatManagerWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeDocumentTextFormats()
    {
        var document = new CadDocument();

        var viewModel = new TextFormatManagerWindowViewModel(document);

        Assert.Equal(
            document.TextFormats.Count,
            viewModel.Formats.Count);
        Assert.Contains(
            viewModel.Formats,
            format => format.Id == TextFormatId.Standard);
    }

    [Fact]
    public void AddFormat_ShouldCreateLargeReadableUserFormat()
    {
        var document = new CadDocument();
        var viewModel = new TextFormatManagerWindowViewModel(document);

        viewModel.AddFormat();

        EditableTextFormatViewModel added = viewModel.SelectedFormat!;

        Assert.False(added.IsBuiltIn);
        Assert.Equal("Nuovo formato testo", added.Name);
        Assert.Equal("Arial", added.FontFamily);
        Assert.Equal("10", added.HeightText);
        Assert.Equal("#FFFFFF", added.ColorHex);
        Assert.False(added.IsBold);
        Assert.False(added.IsItalic);
    }

    [Fact]
    public void DeleteSelectedFormat_WhenBuiltIn_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new TextFormatManagerWindowViewModel(document);
        viewModel.SelectedFormat = viewModel.Formats.Single(format => format.Id == TextFormatId.Standard);

        int before = viewModel.Formats.Count;

        viewModel.DeleteSelectedFormat();

        Assert.Equal(before, viewModel.Formats.Count);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void DeleteSelectedFormat_WhenUsedByTextEntity_ShouldReject()
    {
        var document = new CadDocument();
        var customFormat = new TextFormat(
            new TextFormatId("custom"),
            "Custom",
            "Arial",
            12,
            CadColor.FromRgb(1, 2, 3));

        document.TextFormats.ReplaceAll(document.TextFormats.All.Append(customFormat));
        document.AddEntity(new TextEntity(
            new Point2D(1, 2),
            "Label",
            textFormatId: customFormat.Id));

        var viewModel = new TextFormatManagerWindowViewModel(document);
        viewModel.SelectedFormat = viewModel.Formats.Single(format => format.Id == customFormat.Id);

        viewModel.DeleteSelectedFormat();

        Assert.Contains(viewModel.Formats, format => format.Id == customFormat.Id);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void TryBuildResult_ShouldReturnEditedFormats()
    {
        var document = new CadDocument();
        var viewModel = new TextFormatManagerWindowViewModel(document);
        EditableTextFormatViewModel standard = viewModel.Formats.Single(format => format.Id == TextFormatId.Standard);

        standard.Name = "Testo standard";
        standard.FontFamily = "Consolas";
        standard.HeightText = "14.5";
        standard.ColorHex = "#112233";
        standard.IsBold = true;
        standard.IsItalic = true;

        bool success = viewModel.TryBuildResult(out TextFormatManagerResult result);

        TextFormat edited = result.TextFormats.Single(format => format.Id == TextFormatId.Standard);

        Assert.True(success);
        Assert.Equal("Testo standard", edited.Name);
        Assert.Equal("Consolas", edited.FontFamily);
        Assert.Equal(14.5, edited.Height);
        Assert.Equal(0x11, edited.Color.R);
        Assert.Equal(0x22, edited.Color.G);
        Assert.Equal(0x33, edited.Color.B);
        Assert.True(edited.IsBold);
        Assert.True(edited.IsItalic);
    }

    [Fact]
    public void EditableFormat_ColorPickerColor_ShouldUpdateColorHex()
    {
        var document = new CadDocument();
        var viewModel = new TextFormatManagerWindowViewModel(document);
        EditableTextFormatViewModel standard = viewModel.Formats.Single(format => format.Id == TextFormatId.Standard);

        standard.Color = Color.FromRgb(0x44, 0x55, 0x66);

        Assert.Equal("#445566", standard.ColorHex);
    }

    [Fact]
    public void EditableFormat_ColorHex_ShouldUpdateColorPickerColor()
    {
        var document = new CadDocument();
        var viewModel = new TextFormatManagerWindowViewModel(document);
        EditableTextFormatViewModel standard = viewModel.Formats.Single(format => format.Id == TextFormatId.Standard);

        standard.ColorHex = "#112233";

        Assert.Equal(Color.FromRgb(0x11, 0x22, 0x33), standard.Color);
    }

    [Fact]
    public void TryBuildResult_WithDuplicateNames_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new TextFormatManagerWindowViewModel(document);

        viewModel.Formats.Single(format => format.Id == TextFormatId.Standard).Name = "Same";
        viewModel.Formats.Single(format => format.Id == TextFormatId.Title).Name = "same";

        bool success = viewModel.TryBuildResult(out _);

        Assert.False(success);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void TryBuildResult_WithInvalidHeight_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new TextFormatManagerWindowViewModel(document);

        viewModel.Formats.Single(format => format.Id == TextFormatId.Standard).HeightText = "0";

        bool success = viewModel.TryBuildResult(out _);

        Assert.False(success);
        Assert.True(viewModel.HasValidationMessage);
    }
}
