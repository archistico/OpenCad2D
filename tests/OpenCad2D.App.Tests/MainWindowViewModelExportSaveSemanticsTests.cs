using OpenCad2D.App.ViewModels;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelExportSaveSemanticsTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "OpenCad2D.Tests",
        Guid.NewGuid().ToString("N"));

    public MainWindowViewModelExportSaveSemanticsTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void ExportSvgToFile_WhenDocumentIsDirty_ShouldNotUpdateCurrentFilePathOrClearDirtyState()
    {
        var viewModel = CreateSavedDirtyViewModel(out string nativePath);
        string exportPath = Path.Combine(_directory, "drawing.svg");

        viewModel.ExportSvgToFile(exportPath);

        Assert.Equal(nativePath, viewModel.CurrentFilePath);
        Assert.True(viewModel.IsDirty);
        Assert.Contains("This export does not save the editable OpenCad2D project.", viewModel.LastMessage);
        Assert.Contains("Unsaved project changes remain", viewModel.LastMessage);
    }

    [Fact]
    public void ExportDxfToFile_WhenDocumentIsDirty_ShouldNotUpdateCurrentFilePathOrClearDirtyState()
    {
        var viewModel = CreateSavedDirtyViewModel(out string nativePath);
        string exportPath = Path.Combine(_directory, "drawing.dxf");

        viewModel.ExportDxfToFile(exportPath);

        Assert.Equal(nativePath, viewModel.CurrentFilePath);
        Assert.True(viewModel.IsDirty);
        Assert.Contains("This export does not save the editable OpenCad2D project.", viewModel.LastMessage);
        Assert.Contains("Unsaved project changes remain", viewModel.LastMessage);
    }

    [Fact]
    public void ExportPdfToFile_WhenDocumentIsDirty_ShouldNotUpdateCurrentFilePathOrClearDirtyState()
    {
        var viewModel = CreateSavedDirtyViewModel(out string nativePath);
        string exportPath = Path.Combine(_directory, "drawing.pdf");

        viewModel.ExportPdfToFile(exportPath);

        Assert.Equal(nativePath, viewModel.CurrentFilePath);
        Assert.True(viewModel.IsDirty);
        Assert.Contains("This export does not save the editable OpenCad2D project.", viewModel.LastMessage);
        Assert.Contains("Unsaved project changes remain", viewModel.LastMessage);
    }

    [Fact]
    public void ExportSvgToFile_WhenNativeDocumentIsAlreadySaved_ShouldExplainNativeDrawingIsSaved()
    {
        var viewModel = new MainWindowViewModel();
        string nativePath = Path.Combine(_directory, "drawing.opencad2d.json");
        string exportPath = Path.Combine(_directory, "drawing.svg");

        viewModel.SaveToFile(
            nativePath,
            new ViewportStateDto());

        viewModel.ExportSvgToFile(exportPath);

        Assert.Equal(nativePath, viewModel.CurrentFilePath);
        Assert.False(viewModel.IsDirty);
        Assert.Contains("This export does not save the editable OpenCad2D project.", viewModel.LastMessage);
        Assert.Contains("The native drawing is already saved.", viewModel.LastMessage);
    }


    [Fact]
    public void ExportSvgToFile_WhenNativeDocumentWasNeverSaved_ShouldExplainSaveAsIsStillNeeded()
    {
        var viewModel = new MainWindowViewModel();
        string exportPath = Path.Combine(_directory, "drawing.svg");

        viewModel.ExportSvgToFile(exportPath);

        Assert.Null(viewModel.CurrentFilePath);
        Assert.False(viewModel.IsDirty);
        Assert.Contains("This export does not save the editable OpenCad2D project.", viewModel.LastMessage);
        Assert.Contains("has not been saved yet", viewModel.LastMessage);
        Assert.Contains("Save As", viewModel.LastMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private MainWindowViewModel CreateSavedDirtyViewModel(out string nativePath)
    {
        var viewModel = new MainWindowViewModel();
        nativePath = Path.Combine(_directory, "drawing.opencad2d.json");

        viewModel.SaveToFile(
            nativePath,
            new ViewportStateDto());
        viewModel.Workspace.MarkDocumentChanged();

        Assert.Equal(nativePath, viewModel.CurrentFilePath);
        Assert.True(viewModel.IsDirty);

        return viewModel;
    }
}
