using OpenCad2D.App.Settings;

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
