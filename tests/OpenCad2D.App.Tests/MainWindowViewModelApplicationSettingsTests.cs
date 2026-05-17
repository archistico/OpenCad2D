using OpenCad2D.App.Settings;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelApplicationSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "OpenCad2D.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveToFile_ShouldUpdateLocalApplicationSettings()
    {
        var store = new InMemoryApplicationSettingsStore();
        var viewModel = new MainWindowViewModel(applicationSettingsStore: store);
        string filePath = Path.Combine(_directory, "drawing.opencad2d.json");
        Directory.CreateDirectory(_directory);

        viewModel.SaveToFile(
            filePath,
            new ViewportStateDto());

        Assert.Equal(_directory, store.SavedSettings?.LastSaveDirectory);
        Assert.Equal(new[] { filePath }, store.SavedSettings?.RecentFiles);
    }

    [Fact]
    public void ExportSvgToFile_ShouldUpdateLastExportDirectoryWithoutAddingRecentFile()
    {
        var store = new InMemoryApplicationSettingsStore();
        var viewModel = new MainWindowViewModel(applicationSettingsStore: store);
        string filePath = Path.Combine(_directory, "drawing.svg");
        Directory.CreateDirectory(_directory);

        viewModel.ExportSvgToFile(filePath);

        Assert.Equal(_directory, store.SavedSettings?.LastExportDirectory);
        Assert.Empty(store.SavedSettings?.RecentFiles ?? new List<string> { "unexpected" });
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class InMemoryApplicationSettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = new();

        public ApplicationSettings? SavedSettings { get; private set; }

        public ApplicationSettings Load()
        {
            return _settings;
        }

        public void Save(ApplicationSettings settings)
        {
            SavedSettings = new ApplicationSettings
            {
                SchemaVersion = settings.SchemaVersion,
                LastOpenedFilePath = settings.LastOpenedFilePath,
                LastOpenDirectory = settings.LastOpenDirectory,
                LastSaveDirectory = settings.LastSaveDirectory,
                LastExportDirectory = settings.LastExportDirectory,
                RecentFiles = settings.RecentFiles.ToList()
            };

            _settings = SavedSettings;
        }
    }
}
