using OpenCad2D.App.ViewModels.Pdf;
using OpenCad2D.Export.Pdf;

namespace OpenCad2D.App.Tests.Pdf;

public sealed class PdfExportSettingsWindowViewModelTests
{
    [Fact]
    public void CreateOptions_WithDefaultValues_ShouldReturnDefaultPdfOptions()
    {
        var viewModel = new PdfExportSettingsWindowViewModel();

        PdfExportOptions options = viewModel.CreateOptions();

        Assert.Equal(PdfPageSize.A4, options.PageSize);
        Assert.Equal(PdfPageOrientation.Portrait, options.Orientation);
        Assert.Equal(10.0, options.MarginMillimeters);
        Assert.False(options.IncludeHiddenLayers);
        Assert.True(options.UsePrintFriendlyColors);
    }

    [Fact]
    public void CreateOptions_WithCustomValues_ShouldReturnSelectedOptions()
    {
        var viewModel = new PdfExportSettingsWindowViewModel
        {
            SelectedPageSize = PdfPageSize.A3,
            SelectedOrientation = PdfPageOrientation.Landscape,
            MarginMillimetersText = "15.5",
            IncludeHiddenLayers = true,
            UsePrintFriendlyColors = false
        };

        PdfExportOptions options = viewModel.CreateOptions();

        Assert.Equal(PdfPageSize.A3, options.PageSize);
        Assert.Equal(PdfPageOrientation.Landscape, options.Orientation);
        Assert.Equal(15.5, options.MarginMillimeters);
        Assert.True(options.IncludeHiddenLayers);
        Assert.False(options.UsePrintFriendlyColors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("101")]
    public void CreateOptions_WithInvalidMargin_ShouldThrow(string margin)
    {
        var viewModel = new PdfExportSettingsWindowViewModel
        {
            MarginMillimetersText = margin
        };

        Assert.Throws<ArgumentException>(() => viewModel.CreateOptions());
    }
}
