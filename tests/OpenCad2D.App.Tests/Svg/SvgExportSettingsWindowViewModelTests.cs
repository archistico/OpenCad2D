using OpenCad2D.App.ViewModels.Svg;
using OpenCad2D.Export.Svg;

namespace OpenCad2D.App.Tests.Svg;

public sealed class SvgExportSettingsWindowViewModelTests
{
    [Fact]
    public void CreateOptions_WhenTransparentBackgroundIsSelected_ShouldDisableBackground()
    {
        var viewModel = new SvgExportSettingsWindowViewModel
        {
            SelectedBackgroundMode = SvgBackgroundMode.Transparent,
            MarginText = "15",
            GroupByLayer = true,
            IncludeHiddenLayers = false,
            IncludeMetadata = true
        };

        SvgExportOptions options = viewModel.CreateOptions("Drawing");

        Assert.Equal(SvgBackgroundMode.Transparent, options.BackgroundMode);
        Assert.False(options.IncludeBackground);
        Assert.True(options.GroupByLayer);
        Assert.Equal(15, options.Margin);
        Assert.Equal("Drawing", options.Title);
    }

    [Fact]
    public void CreateOptions_WhenMarginIsNegative_ShouldThrow()
    {
        var viewModel = new SvgExportSettingsWindowViewModel
        {
            MarginText = "-1"
        };

        Assert.Throws<ArgumentException>(() => viewModel.CreateOptions("Drawing"));
    }

    [Fact]
    public void Constructor_WhenOptionsDisableBackground_ShouldSelectTransparentBackground()
    {
        var viewModel = new SvgExportSettingsWindowViewModel(new SvgExportOptions
        {
            IncludeBackground = false,
            GroupByLayer = true
        });

        Assert.Equal(SvgBackgroundMode.Transparent, viewModel.SelectedBackgroundMode);
        Assert.True(viewModel.GroupByLayer);
    }
}
