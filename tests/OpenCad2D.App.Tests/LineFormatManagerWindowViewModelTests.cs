using System.Linq;
using OpenCad2D.App.ViewModels.LineFormats;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.App.Tests;

public sealed class LineFormatManagerWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeDocumentLineFormats()
    {
        var document = new CadDocument();

        var viewModel = new LineFormatManagerWindowViewModel(document);

        Assert.Equal(
            document.LineFormats.Count,
            viewModel.Formats.Count);
        Assert.Contains(
            viewModel.Formats,
            format => format.Id == LineFormatId.Continuous);
    }

    [Fact]
    public void AddFormat_ShouldCreateEditableUserFormat()
    {
        var document = new CadDocument();
        var viewModel = new LineFormatManagerWindowViewModel(document);

        viewModel.AddFormat();

        EditableLineFormatViewModel added = viewModel.SelectedFormat!;

        Assert.False(added.IsBuiltIn);
        Assert.Equal("Nuovo formato", added.Name);
        Assert.Equal("#FFFFFF", added.ColorHex);
        Assert.Equal("1", added.LineWeightText);
        Assert.Equal(LineStyle.Continuous, added.LineStyle);
    }

    [Fact]
    public void DeleteSelectedFormat_WhenBuiltIn_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new LineFormatManagerWindowViewModel(document);
        viewModel.SelectedFormat = viewModel.Formats.Single(format => format.Id == LineFormatId.Continuous);

        int before = viewModel.Formats.Count;

        viewModel.DeleteSelectedFormat();

        Assert.Equal(before, viewModel.Formats.Count);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void DeleteSelectedFormat_WhenUsedByLayer_ShouldReject()
    {
        var document = new CadDocument();
        var customFormat = new LineFormat(
            new LineFormatId("custom"),
            "Custom",
            CadColor.FromRgb(1, 2, 3),
            LineWeight.FromMillimeters(2),
            LineStyle.Dashed);

        document.LineFormats.ReplaceAll(document.LineFormats.All.Append(customFormat));
        document.Layers.Add(new Layer(
            new LayerId("custom-layer"),
            "Custom layer",
            customFormat.Id));

        var viewModel = new LineFormatManagerWindowViewModel(document);
        viewModel.SelectedFormat = viewModel.Formats.Single(format => format.Id == customFormat.Id);

        viewModel.DeleteSelectedFormat();

        Assert.Contains(viewModel.Formats, format => format.Id == customFormat.Id);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void TryBuildResult_ShouldReturnEditedFormats()
    {
        var document = new CadDocument();
        var viewModel = new LineFormatManagerWindowViewModel(document);
        EditableLineFormatViewModel continuous = viewModel.Formats.Single(format => format.Id == LineFormatId.Continuous);

        continuous.Name = "Bianca continua";
        continuous.ColorHex = "#112233";
        continuous.LineWeightText = "2.5";
        continuous.LineStyle = LineStyle.Dashed;

        bool success = viewModel.TryBuildResult(out LineFormatManagerResult result);

        LineFormat edited = result.LineFormats.Single(format => format.Id == LineFormatId.Continuous);

        Assert.True(success);
        Assert.Equal("Bianca continua", edited.Name);
        Assert.Equal(0x11, edited.Color.R);
        Assert.Equal(0x22, edited.Color.G);
        Assert.Equal(0x33, edited.Color.B);
        Assert.Equal(2.5, edited.LineWeight.Millimeters);
        Assert.Equal(LineStyle.Dashed, edited.LineStyle);
    }

    [Fact]
    public void TryBuildResult_WithDuplicateNames_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new LineFormatManagerWindowViewModel(document);

        viewModel.Formats.Single(format => format.Id == LineFormatId.Continuous).Name = "Same";
        viewModel.Formats.Single(format => format.Id == LineFormatId.Axis).Name = "same";

        bool success = viewModel.TryBuildResult(out _);

        Assert.False(success);
        Assert.True(viewModel.HasValidationMessage);
    }
}
