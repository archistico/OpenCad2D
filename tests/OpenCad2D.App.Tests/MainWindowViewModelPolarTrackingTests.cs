using System.Linq;
using OpenCad2D.App.ViewModels;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelPolarTrackingTests
{
    [Fact]
    public void Constructor_ShouldExposeDefaultPolarTrackingOptions()
    {
        var viewModel = new MainWindowViewModel();

        string[] labels = viewModel.PolarTrackingOptions
            .Select(option => option.DisplayName)
            .ToArray();

        Assert.Equal(
            new[] { "Off", "90°", "45°", "30°", "15°" },
            labels);
    }

    [Fact]
    public void Constructor_ShouldStartWithPolarTrackingOff()
    {
        var viewModel = new MainWindowViewModel();

        Assert.False(viewModel.Workspace.AngleConstraintSettings.IsEnabled);
        Assert.Equal("Polar: Off", viewModel.PolarTrackingText);
        Assert.Same(
            viewModel.PolarTrackingOptions[0],
            viewModel.SelectedPolarTrackingOption);
    }

    [Fact]
    public void SetPolarTracking_ShouldApplySelectedStepToWorkspace()
    {
        var viewModel = new MainWindowViewModel();
        var option = viewModel.PolarTrackingOptions.Single(item => item.DisplayName == "45°");

        viewModel.SetPolarTracking(option);

        Assert.True(viewModel.Workspace.AngleConstraintSettings.IsEnabled);
        Assert.Equal(45, viewModel.Workspace.AngleConstraintSettings.StepDegrees);
        Assert.False(viewModel.Workspace.Context.IsOrthoEnabled);
        Assert.Equal("Polar: 45°", viewModel.PolarTrackingText);
        Assert.Same(option, viewModel.SelectedPolarTrackingOption);
    }

    [Fact]
    public void SetPolarTracking_Off_ShouldDisableWorkspaceConstraint()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetPolarTracking(viewModel.PolarTrackingOptions.Single(item => item.DisplayName == "30°"));
        viewModel.SetPolarTracking(viewModel.PolarTrackingOptions.Single(item => item.DisplayName == "Off"));

        Assert.False(viewModel.Workspace.AngleConstraintSettings.IsEnabled);
        Assert.Equal("Polar: Off", viewModel.PolarTrackingText);
    }

    [Fact]
    public void SetOrthoEnabled_WhenEnabled_ShouldResetPolarTrackingToOff()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SetPolarTracking(viewModel.PolarTrackingOptions.Single(item => item.DisplayName == "15°"));

        viewModel.SetOrthoEnabled(true);

        Assert.True(viewModel.Workspace.Context.IsOrthoEnabled);
        Assert.False(viewModel.Workspace.AngleConstraintSettings.IsEnabled);
        Assert.Equal("Polar: Off", viewModel.PolarTrackingText);
        Assert.Same(
            viewModel.PolarTrackingOptions[0],
            viewModel.SelectedPolarTrackingOption);
    }
}
