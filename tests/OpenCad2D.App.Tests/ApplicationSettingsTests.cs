using OpenCad2D.App.Settings;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.App.Tests;

public sealed class ApplicationSettingsTests
{
    [Fact]
    public void RegisterOpenedFile_ShouldUpdateLastOpenedDirectoryAndRecentFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "OpenCad2D", "Drawings");
        string filePath = Path.Combine(directory, "house.opencad2d.json");
        var settings = new ApplicationSettings();

        settings.RegisterOpenedFile(filePath);

        Assert.Equal(filePath, settings.LastOpenedFilePath);
        Assert.Equal(directory, settings.LastOpenDirectory);
        Assert.Equal(new[] { filePath }, settings.RecentFiles);
    }

    [Fact]
    public void RegisterSavedFile_ShouldMoveExistingRecentFileToTopWithoutDuplicates()
    {
        string directory = Path.Combine(Path.GetTempPath(), "OpenCad2D", "Drawings");
        string firstFilePath = Path.Combine(directory, "a.opencad2d.json");
        string secondFilePath = Path.Combine(directory, "b.opencad2d.json");
        var settings = new ApplicationSettings
        {
            RecentFiles = new List<string>
            {
                firstFilePath,
                secondFilePath
            }
        };

        settings.RegisterSavedFile(secondFilePath);

        Assert.Equal(directory, settings.LastSaveDirectory);
        Assert.Equal(
            new[]
            {
                secondFilePath,
                firstFilePath
            },
            settings.RecentFiles);
    }

    [Fact]
    public void Normalize_ShouldTrimRecentFilesAndLimitList()
    {
        var settings = new ApplicationSettings
        {
            SchemaVersion = -1,
            RecentFiles = Enumerable.Range(0, 12)
                .Select(index => $" {Path.Combine(Path.GetTempPath(), "OpenCad2D", "Drawings", $"{index}.opencad2d.json")} ")
                .ToList()
        };

        settings.Normalize();

        Assert.Equal(ApplicationSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Equal(ApplicationSettings.DefaultRecentFileLimit, settings.RecentFiles.Count);
        Assert.Equal(
            Path.Combine(Path.GetTempPath(), "OpenCad2D", "Drawings", "0.opencad2d.json"),
            settings.RecentFiles[0]);
    }


    [Fact]
    public void CaptureDraftingDefaults_ShouldStoreGridAndSnapSettings()
    {
        var settings = new ApplicationSettings();
        var grid = new GridSettings(
            step: 2,
            originX: 1,
            originY: 3,
            isVisible: false,
            majorStep: 10,
            minimumScreenSpacing: 6,
            maximumScreenSpacing: 120,
            kind: GridKind.Isometric,
            isometricAngleDegrees: 35);

        settings.CaptureDraftingDefaults(
            grid,
            SnapKind.Endpoint | SnapKind.Grid,
            12);

        Assert.NotNull(settings.DefaultGrid);
        Assert.Equal("Isometric", settings.DefaultGrid.Kind);
        Assert.False(settings.DefaultGrid.IsVisible);
        Assert.Equal(2, settings.DefaultGrid.MinorStep);
        Assert.NotNull(settings.DefaultSnapping);
        Assert.True(settings.DefaultSnapping.IsEnabled);
        Assert.Equal(12, settings.DefaultSnapping.Tolerance);
        Assert.Contains("Endpoint", settings.DefaultSnapping.EnabledModes);
        Assert.Contains("Grid", settings.DefaultSnapping.EnabledModes);
    }

    [Fact]
    public void Normalize_WithInvalidDraftingDefaults_ShouldUseSafeValues()
    {
        var settings = new ApplicationSettings
        {
            DefaultGrid = new ApplicationGridSettings
            {
                Kind = "Invalid",
                MinorStep = -1,
                MajorStep = 0,
                MinimumScreenSpacing = -1,
                MaximumScreenSpacing = -1,
                IsometricAngleDegrees = 180
            },
            DefaultSnapping = new ApplicationSnapSettings
            {
                Tolerance = -1,
                EnabledModes = new List<string>
                {
                    "Endpoint",
                    "Invalid"
                }
            }
        };

        settings.Normalize();

        Assert.Equal("Rectangular", settings.DefaultGrid!.Kind);
        Assert.Equal(10, settings.DefaultGrid.MinorStep);
        Assert.True(settings.DefaultGrid.MaximumScreenSpacing > settings.DefaultGrid.MinimumScreenSpacing);
        Assert.Equal(30, settings.DefaultGrid.IsometricAngleDegrees);
        Assert.Equal(8, settings.DefaultSnapping!.Tolerance);
        Assert.Equal(new[] { "Endpoint" }, settings.DefaultSnapping.EnabledModes);
    }

    [Fact]
    public void RegisterExportedFile_ShouldOnlyUpdateLastExportDirectory()
    {
        var settings = new ApplicationSettings();

        string directory = Path.Combine(Path.GetTempPath(), "OpenCad2D", "Exports");
        string filePath = Path.Combine(directory, "drawing.svg");

        settings.RegisterExportedFile(filePath);

        Assert.Equal(directory, settings.LastExportDirectory);
        Assert.Empty(settings.RecentFiles);
    }
}
