using OpenCad2D.App.Settings;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.App.Tests;

public sealed class JsonApplicationSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "OpenCad2D.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldReturnDefaultSettings()
    {
        var store = new JsonApplicationSettingsStore(Path.Combine(_directory, "settings.json"));

        ApplicationSettings settings = store.Load();

        Assert.Equal(ApplicationSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Empty(settings.RecentFiles);
    }

    [Fact]
    public void SaveAndLoad_ShouldRoundTripSettings()
    {
        string path = Path.Combine(_directory, "settings.json");
        var store = new JsonApplicationSettingsStore(path);
        string drawingsDirectory = Path.Combine(_directory, "Drawings");
        string exportsDirectory = Path.Combine(_directory, "Exports");
        string drawingPath = Path.Combine(drawingsDirectory, "house.opencad2d.json");
        string exportPath = Path.Combine(exportsDirectory, "house.svg");
        var settings = new ApplicationSettings();
        settings.RegisterOpenedFile(drawingPath);
        settings.RegisterExportedFile(exportPath);
        settings.CaptureDraftingDefaults(
            new GridSettings(
                step: 5,
                originX: 1,
                originY: 2,
                isVisible: false,
                majorStep: 25,
                kind: GridKind.Isometric),
            SnapKind.Endpoint | SnapKind.Grid,
            11);

        store.Save(settings);
        ApplicationSettings loaded = store.Load();

        Assert.Equal(drawingPath, loaded.LastOpenedFilePath);
        Assert.Equal(drawingsDirectory, loaded.LastOpenDirectory);
        Assert.Equal(exportsDirectory, loaded.LastExportDirectory);
        Assert.Equal(new[] { drawingPath }, loaded.RecentFiles);
        Assert.NotNull(loaded.DefaultGrid);
        Assert.Equal("Isometric", loaded.DefaultGrid.Kind);
        Assert.False(loaded.DefaultGrid.IsVisible);
        Assert.NotNull(loaded.DefaultSnapping);
        Assert.Equal(11, loaded.DefaultSnapping.Tolerance);
        Assert.Contains("Endpoint", loaded.DefaultSnapping.EnabledModes);
        Assert.Contains("Grid", loaded.DefaultSnapping.EnabledModes);
    }

    [Fact]
    public void Load_WhenJsonIsInvalid_ShouldReturnDefaultSettings()
    {
        string path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{ invalid json");
        var store = new JsonApplicationSettingsStore(path);

        ApplicationSettings settings = store.Load();

        Assert.Equal(ApplicationSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Empty(settings.RecentFiles);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
