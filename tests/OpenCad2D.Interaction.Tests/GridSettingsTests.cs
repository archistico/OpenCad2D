using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class GridSettingsTests
{
    [Fact]
    public void Default_ShouldUseExpectedRenderingSettings()
    {
        var settings = new GridSettings();

        Assert.True(settings.IsVisible);
        Assert.Equal(10, settings.MinorStep);
        Assert.Equal(10, settings.Step);
        Assert.Equal(50, settings.MajorStep);
        Assert.Equal(8, settings.MinimumScreenSpacing);
        Assert.Equal(220, settings.MaximumScreenSpacing);
        Assert.Equal(GridKind.Rectangular, settings.Kind);
        Assert.Equal(30, settings.IsometricAngleDegrees);
    }

    [Fact]
    public void WithVisibility_ShouldReturnSettingsWithUpdatedVisibility()
    {
        var settings = new GridSettings();

        GridSettings result = settings.WithVisibility(false);

        Assert.False(result.IsVisible);
        Assert.Equal(settings.MinorStep, result.MinorStep);
        Assert.Equal(settings.MajorStep, result.MajorStep);
    }

    [Fact]
    public void WithSpacing_ShouldReturnSettingsWithUpdatedMinorAndMajorStep()
    {
        var settings = new GridSettings();

        GridSettings result = settings.WithSpacing(
            minorStep: 5,
            majorStep: 25);

        Assert.Equal(5, result.MinorStep);
        Assert.Equal(5, result.Step);
        Assert.Equal(25, result.MajorStep);
    }

    [Fact]
    public void WithKind_ShouldReturnSettingsWithUpdatedKindAndAngle()
    {
        var settings = new GridSettings();

        GridSettings result = settings.WithKind(
            GridKind.Isometric,
            isometricAngleDegrees: 30);

        Assert.Equal(GridKind.Isometric, result.Kind);
        Assert.Equal(30, result.IsometricAngleDegrees);
        Assert.Equal(settings.MinorStep, result.MinorStep);
    }

    [Fact]
    public void WithOrigin_ShouldReturnSettingsWithUpdatedOrigin()
    {
        var settings = new GridSettings();

        GridSettings result = settings.WithOrigin(
            originX: 12,
            originY: -8);

        Assert.Equal(12, result.OriginX);
        Assert.Equal(-8, result.OriginY);
        Assert.Equal(settings.Kind, result.Kind);
    }

    [Fact]
    public void ShouldRenderScreenSpacing_ShouldRespectConfiguredRange()
    {
        var settings = new GridSettings(
            minimumScreenSpacing: 8,
            maximumScreenSpacing: 220);

        Assert.False(settings.ShouldRenderScreenSpacing(7.99));
        Assert.True(settings.ShouldRenderScreenSpacing(8));
        Assert.True(settings.ShouldRenderScreenSpacing(220));
        Assert.False(settings.ShouldRenderScreenSpacing(220.01));
    }

    [Fact]
    public void Constructor_WithInvalidMajorStep_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GridSettings(majorStep: 0));
    }

    [Fact]
    public void Constructor_WithMajorStepSmallerThanMinorStep_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GridSettings(
                step: 10,
                majorStep: 5));
    }

    [Fact]
    public void Constructor_WithInvalidScreenSpacingRange_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GridSettings(
                minimumScreenSpacing: 10,
                maximumScreenSpacing: 10));
    }

    [Fact]
    public void Constructor_WithInvalidIsometricAngle_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GridSettings(
                kind: GridKind.Isometric,
                isometricAngleDegrees: 0));
    }
    [Fact]
    public void GetIsometricVerticalStep_WithThirtyDegreeAngle_ShouldAlignWithDiagonalVertices()
    {
        var settings = new GridSettings(
            step: 10,
            kind: GridKind.Isometric,
            isometricAngleDegrees: 30);

        double verticalStep = settings.GetIsometricVerticalStep(10);

        Assert.True(Math.Abs(verticalStep - 8.660254037844387) < 0.000001);
    }

}
