using OpenCad2D.App.Settings;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Interaction.Snapping;
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


    [Fact]
    public void ApplyGridSettings_ShouldPersistLocalDraftingDefaults()
    {
        var store = new InMemoryApplicationSettingsStore();
        var viewModel = new MainWindowViewModel(applicationSettingsStore: store);
        var grid = new GridSettings(
            step: 2,
            isVisible: false,
            majorStep: 10,
            kind: GridKind.Isometric);

        viewModel.ApplyGridSettings(grid);

        Assert.NotNull(store.SavedSettings?.DefaultGrid);
        Assert.Equal("Isometric", store.SavedSettings.DefaultGrid.Kind);
        Assert.False(store.SavedSettings.DefaultGrid.IsVisible);
        Assert.Equal(2, store.SavedSettings.DefaultGrid.MinorStep);
    }

    [Fact]
    public void Constructor_WithSavedDraftingDefaults_ShouldApplyThemToDefaultDocument()
    {
        var store = new InMemoryApplicationSettingsStore();
        store.Seed(new ApplicationSettings
        {
            DefaultGrid = new ApplicationGridSettings
            {
                Kind = GridKind.Isometric.ToString(),
                IsVisible = false,
                MinorStep = 3,
                MajorStep = 12,
                MinimumScreenSpacing = 8,
                MaximumScreenSpacing = 220,
                IsometricAngleDegrees = 30
            },
            DefaultSnapping = new ApplicationSnapSettings
            {
                IsEnabled = true,
                EnabledModes = new List<string>
                {
                    SnapKind.Endpoint.ToString(),
                    SnapKind.Grid.ToString()
                },
                Tolerance = 13
            }
        });

        var viewModel = new MainWindowViewModel(applicationSettingsStore: store);

        Assert.Equal(GridKind.Isometric, viewModel.Workspace.GridSettings.Kind);
        Assert.False(viewModel.Workspace.GridSettings.IsVisible);
        Assert.Equal(3, viewModel.Workspace.GridSettings.MinorStep);
        Assert.Equal(SnapKind.Endpoint | SnapKind.Grid, viewModel.Workspace.Context.EnabledSnaps);
        Assert.Equal(13, viewModel.Workspace.Context.SnapTolerance);
    }

    [Fact]
    public void Constructor_WithNoSavedSnappingDefaults_ShouldKeepNearestDisabledByDefault()
    {
        var store = new InMemoryApplicationSettingsStore();

        var viewModel = new MainWindowViewModel(applicationSettingsStore: store);

        Assert.True(viewModel.IsSnapEnabled(SnapKind.Endpoint));
        Assert.True(viewModel.IsSnapEnabled(SnapKind.Grid));
        Assert.False(viewModel.IsSnapEnabled(SnapKind.Nearest));
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

        public void Seed(ApplicationSettings settings)
        {
            _settings = Clone(settings);
        }

        public ApplicationSettings Load()
        {
            return _settings;
        }

        public void Save(ApplicationSettings settings)
        {
            SavedSettings = Clone(settings);

            _settings = SavedSettings;
        }

        private static ApplicationSettings Clone(ApplicationSettings settings)
        {
            return new ApplicationSettings
            {
                SchemaVersion = settings.SchemaVersion,
                LastOpenedFilePath = settings.LastOpenedFilePath,
                LastOpenDirectory = settings.LastOpenDirectory,
                LastSaveDirectory = settings.LastSaveDirectory,
                LastExportDirectory = settings.LastExportDirectory,
                ReopenLastFileOnStartup = settings.ReopenLastFileOnStartup,
                DefaultGrid = settings.DefaultGrid is null
                    ? null
                    : new ApplicationGridSettings
                    {
                        Kind = settings.DefaultGrid.Kind,
                        IsVisible = settings.DefaultGrid.IsVisible,
                        MinorStep = settings.DefaultGrid.MinorStep,
                        MajorStep = settings.DefaultGrid.MajorStep,
                        OriginX = settings.DefaultGrid.OriginX,
                        OriginY = settings.DefaultGrid.OriginY,
                        MinimumScreenSpacing = settings.DefaultGrid.MinimumScreenSpacing,
                        MaximumScreenSpacing = settings.DefaultGrid.MaximumScreenSpacing,
                        IsometricAngleDegrees = settings.DefaultGrid.IsometricAngleDegrees
                    },
                DefaultSnapping = settings.DefaultSnapping is null
                    ? null
                    : new ApplicationSnapSettings
                    {
                        IsEnabled = settings.DefaultSnapping.IsEnabled,
                        EnabledModes = settings.DefaultSnapping.EnabledModes.ToList(),
                        Tolerance = settings.DefaultSnapping.Tolerance
                    },
                RecentFiles = settings.RecentFiles.ToList()
            };
        }
    }
}
