using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using OpenCad2D.Persistence;
using Avalonia.Platform.Storage;
using Avalonia.Layout;
using OpenCad2D.App.Controls;
using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.Layers;
using OpenCad2D.App.ViewModels.Grid;
using OpenCad2D.App.ViewModels.LineFormats;
using OpenCad2D.App.ViewModels.TextFormats;
using OpenCad2D.App.ViewModels.DimensionStyles;
using OpenCad2D.App.ViewModels.PolarTracking;
using OpenCad2D.App.ViewModels.ImageReferences;
using OpenCad2D.App.ViewModels.ImportDrawing;
using OpenCad2D.App.ViewModels.Blocks;
using OpenCad2D.App.ViewModels.Library;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Export.Pdf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Measurements;
using OpenCad2D.Tools.Input;
using System;
using System.Diagnostics;
using System.IO;

namespace OpenCad2D.App;

public partial class MainWindow : Window
{
    private string _commandInputBuffer = string.Empty;
    private TextBox? _activeKeyboardHudField;
    private CommandHudFieldKind? _activeLogicalHudFieldKind;
    private string _activeLogicalHudFieldText = string.Empty;
    private static readonly FilePickerFileType OpenCad2DFileType = new("OpenCad2D drawing")
    {
        Patterns = new[] { "*.opencad2d.json" },
        MimeTypes = new[] { "application/json" }
    };

    private static readonly FilePickerFileType SvgFileType = new("SVG image")
    {
        Patterns = new[] { "*.svg" },
        MimeTypes = new[] { "image/svg+xml" }
    };

    private static readonly FilePickerFileType DxfFileType = new("DXF drawing")
    {
        Patterns = new[] { "*.dxf" },
        MimeTypes = new[] { "application/dxf", "application/x-dxf" }
    };

    private static readonly FilePickerFileType RasterImageFileType = new("Raster image")
    {
        Patterns = new[] { "*.png", "*.jpg", "*.jpeg" },
        MimeTypes = new[] { "image/png", "image/jpeg" }
    };

    private static readonly FilePickerFileType PdfFileType = new("PDF document")
    {
        Patterns = new[] { "*.pdf" },
        MimeTypes = new[] { "application/pdf" }
    };

    private readonly MainWindowViewModel _viewModel;
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel(new AvaloniaTextInputProvider(this));

        DataContext = _viewModel;

        AddHandler(
            InputElement.KeyDownEvent,
            Window_PreviewKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        RefreshAllUiAfterDocumentChange(clearSnapMarker: false, focusCanvas: false);

        Closing += Window_Closing;
    }

    private void RefreshAllUiAfterDocumentChange(
        bool clearSnapMarker = true,
        bool focusCanvas = true)
    {
        InitializeLayerComboBox();
        InitializePolarTrackingComboBox();
        RefreshLayerControls();
        RefreshSnapControls();
        RefreshStatus();

        if (clearSnapMarker)
        {
            CadCanvas.ClearSnapMarker();
        }

        CadCanvas.InvalidateVisual();

        if (focusCanvas)
        {
            CadCanvas.Focus();
        }
    }


    private async void New_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (!await ConfirmProceedWithUnsavedChangesAsync())
        {
            return;
        }

        _viewModel.NewDocument();

        CadCanvas.ResetViewport();
        RefreshAllUiAfterDocumentChange();
    }

    private async void Open_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            if (!await ConfirmProceedWithUnsavedChangesAsync())
            {
                return;
            }

            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Open OpenCad2D drawing",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { OpenCad2DFileType }
                });

            if (files.Count == 0)
            {
                return;
            }

            string? filePath = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Open",
                    "Only local files are supported in this version.");
                return;
            }

            var viewportState = _viewModel.OpenFromFile(filePath);

            CadCanvas.ApplyViewportState(viewportState);
            RefreshAllUiAfterDocumentChange();

            await ShowMissingImageReferencesWarningIfNeededAsync();
        }
        catch (DocumentLoadException exception)
        {
            await ShowMessageAsync(
                "Open failed",
                exception.Message);
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Open failed",
                exception.Message);
        }
    }



    private async void ImportDrawing_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Import OpenCad2D drawing into current document",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { OpenCad2DFileType }
                });

            if (files.Count == 0)
            {
                return;
            }

            string? filePath = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Import Drawing",
                    "Only local OpenCad2D files are supported in this version.");
                return;
            }

            var optionsWindow = new ImportDrawingOptionsWindow();
            OpenCad2DImportPlacementOptions? options = await optionsWindow
                .ShowDialog<OpenCad2DImportPlacementOptions?>(this);

            if (options is null)
            {
                return;
            }

            ToolResult result = _viewModel.BeginImportDrawingPlacementFromFile(
                filePath,
                options);

            BeginPointPlacementSnapping(result);
            RefreshStatus();
            CadCanvas.InvalidateVisual();
            CadCanvas.Focus();

            if (result.Message is not null)
            {
                _viewModel.SetLastResult(result);
            }
        }
        catch (DocumentLoadException exception)
        {
            await ShowMessageAsync(
                "Import Drawing failed",
                exception.Message);
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Import Drawing failed",
                exception.Message);
        }
    }

    private async void ImportDxf_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            if (!await ConfirmProceedWithUnsavedChangesAsync())
            {
                return;
            }

            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Import DXF drawing",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { DxfFileType }
                });

            if (files.Count == 0)
            {
                return;
            }

            string? filePath = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Import DXF",
                    "Only local files are supported in this version.");
                return;
            }

            DxfImportResult result = _viewModel.ImportDxfFromFile(filePath);

            if (result.HasErrors)
            {
                await ShowDxfImportReportAsync(result);
                return;
            }

            CadCanvas.ResetViewport();
            RefreshAllUiAfterDocumentChange();

            if (result.HasWarnings)
            {
                await ShowDxfImportReportAsync(result);
            }
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Import DXF failed",
                exception.Message);
        }
    }

    private async void AttachImage_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Attach raster image",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { RasterImageFileType }
                });

            if (files.Count == 0)
            {
                return;
            }

            string? filePath = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Attach image",
                    "Only local image files are supported in this version.");
                return;
            }

            using var bitmap = new Bitmap(filePath);
            int pixelWidth = bitmap.PixelSize.Width;
            int pixelHeight = bitmap.PixelSize.Height;

            Point2D center = CadCanvas.LastVisibleWorldBounds.Center;
            double visibleWidth = Math.Abs(CadCanvas.LastVisibleWorldBounds.Width);
            double targetWidth = visibleWidth > 1e-9
                ? Math.Max(1.0, visibleWidth * 0.30)
                : Math.Max(1.0, pixelWidth / 10.0);
            double aspectRatio = pixelHeight > 0 && pixelWidth > 0
                ? pixelHeight / (double)pixelWidth
                : 1.0;
            double targetHeight = Math.Max(1.0, targetWidth * aspectRatio);

            ToolResult result = _viewModel.AddImageReference(
                filePath,
                center,
                targetWidth,
                targetHeight,
                pixelWidth,
                pixelHeight);

            RefreshAllUiAfterDocumentChange();
            CadCanvas.InvalidateVisual();

            if (result.Changed)
            {
                RefreshStatus();
            }
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Attach image failed",
                exception.Message);
        }
    }

    private async void ReplaceImage_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_viewModel.HasSingleSelectedImageReference)
        {
            await ShowMessageAsync(
                "Replace image",
                "Select exactly one image reference before replacing or relinking its file.");
            return;
        }

        try
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Replace / relink raster image",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { RasterImageFileType }
                });

            if (files.Count == 0)
            {
                return;
            }

            string? filePath = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Replace image",
                    "Only local image files are supported in this version.");
                return;
            }

            using var bitmap = new Bitmap(filePath);

            ToolResult result = _viewModel.ReplaceSelectedImageReference(
                filePath,
                bitmap.PixelSize.Width,
                bitmap.PixelSize.Height);

            RefreshAllUiAfterDocumentChange();
            CadCanvas.InvalidateVisual();

            if (result.Changed)
            {
                RefreshStatus();
            }
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Replace image failed",
                exception.Message);
        }
    }


    private async void RelinkMissingImage_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_viewModel.HasMissingImageReferences)
        {
            await ShowMessageAsync(
                "Relink missing image",
                "No missing image reference was found in the current drawing.");
            return;
        }

        try
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Relink missing raster image",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { RasterImageFileType }
                });

            if (files.Count == 0)
            {
                return;
            }

            string? filePath = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Relink missing image",
                    "Only local image files are supported in this version.");
                return;
            }

            using var bitmap = new Bitmap(filePath);

            ToolResult result = _viewModel.RelinkFirstMissingImageReference(
                filePath,
                bitmap.PixelSize.Width,
                bitmap.PixelSize.Height);

            RefreshAllUiAfterDocumentChange();
            CadCanvas.InvalidateVisual();

            if (!result.Changed)
            {
                await ShowMessageAsync(
                    "Relink missing image",
                    result.Message ?? "The missing image reference could not be relinked.");
                return;
            }

            RefreshStatus();
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Relink missing image failed",
                exception.Message);
        }
    }


    private async void ResetImageAspect_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ToolResult result = _viewModel.ResetSelectedImageReferenceAspectRatio();

        RefreshAllUiAfterDocumentChange();
        CadCanvas.InvalidateVisual();

        if (!result.Changed)
        {
            await ShowMessageAsync(
                "Reset image aspect",
                result.Message ?? "The selected image aspect ratio could not be reset.");
            return;
        }

        RefreshStatus();
    }

    private async void CollectImageReferences_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            ToolResult result = _viewModel.CollectExternalImageReferences();

            RefreshAllUiAfterDocumentChange();
            CadCanvas.InvalidateVisual();

            if (!result.Changed)
            {
                await ShowMessageAsync(
                    "Collect references",
                    result.Message ?? "No external image reference was collected.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(_viewModel.CurrentFilePath))
            {
                _viewModel.SaveToFile(
                    _viewModel.CurrentFilePath,
                    CadCanvas.GetViewportState());
            }

            RefreshAllUiAfterDocumentChange();
            RefreshStatus();

            await ShowMessageAsync(
                "Collect references",
                result.Message ?? "External image references were collected into the drawing images folder.");
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Collect references failed",
                exception.Message);
        }
    }

    private async void ManageImageReferences_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await ShowImageReferenceManagerAsync();
    }

    private async Task ShowImageReferenceManagerAsync()
    {
        while (true)
        {
            var dialogViewModel = new ImageReferenceManagerWindowViewModel(
                _viewModel.Workspace.Document);

            var dialog = new ImageReferenceManagerWindow(dialogViewModel);

            ImageReferenceManagerResult? result = await dialog.ShowDialog<ImageReferenceManagerResult?>(this);

            if (result?.Reference is null || result.Action == ImageReferenceManagerAction.None)
            {
                CadCanvas.Focus();
                return;
            }

            switch (result.Action)
            {
                case ImageReferenceManagerAction.SelectInDrawing:
                    SelectImageReferenceFromManager(result.Reference.EntityId);
                    return;

                case ImageReferenceManagerAction.Relink:
                    await RelinkImageReferenceFromManagerAsync(result.Reference.EntityId);
                    break;

                case ImageReferenceManagerAction.Replace:
                    await ReplaceImageReferenceFromManagerAsync(result.Reference.EntityId);
                    break;

                case ImageReferenceManagerAction.OpenFolder:
                    await OpenImageReferenceFolderFromManagerAsync(result.Reference);
                    break;

                case ImageReferenceManagerAction.SetTransparency:
                    await SetImageReferenceTransparencyFromManagerAsync(result.Reference.EntityId, result.TransparencyPercent ?? 0.0);
                    break;
            }
        }
    }

    private void SelectImageReferenceFromManager(OpenCad2D.Core.Identifiers.EntityId entityId)
    {
        _viewModel.SelectImageReference(entityId);

        RefreshAllUiAfterDocumentChange();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        RefreshStatus();
        CadCanvas.Focus();
    }

    private async Task RelinkImageReferenceFromManagerAsync(OpenCad2D.Core.Identifiers.EntityId entityId)
    {
        ImageFileSelection? selection = await PickRasterImageAsync("Relink raster image");

        if (selection is null)
        {
            return;
        }

        ToolResult result = _viewModel.RelinkImageReference(
            entityId,
            selection.FilePath,
            selection.PixelWidth,
            selection.PixelHeight);

        RefreshAllUiAfterDocumentChange();
        CadCanvas.InvalidateVisual();
        RefreshStatus();

        if (!result.Changed)
        {
            await ShowMessageAsync(
                "Relink image",
                result.Message ?? "The selected image reference could not be relinked.");
        }
    }

    private async Task ReplaceImageReferenceFromManagerAsync(OpenCad2D.Core.Identifiers.EntityId entityId)
    {
        ImageFileSelection? selection = await PickRasterImageAsync("Replace raster image");

        if (selection is null)
        {
            return;
        }

        ToolResult result = _viewModel.ReplaceImageReference(
            entityId,
            selection.FilePath,
            selection.PixelWidth,
            selection.PixelHeight);

        RefreshAllUiAfterDocumentChange();
        CadCanvas.InvalidateVisual();
        RefreshStatus();

        if (!result.Changed)
        {
            await ShowMessageAsync(
                "Replace image",
                result.Message ?? "The selected image reference could not be replaced.");
        }
    }

    private async Task SetImageReferenceTransparencyFromManagerAsync(
        OpenCad2D.Core.Identifiers.EntityId entityId,
        double transparencyPercent)
    {
        ToolResult result = _viewModel.SetImageReferenceTransparency(
            entityId,
            transparencyPercent);

        RefreshAllUiAfterDocumentChange();
        CadCanvas.InvalidateVisual();
        RefreshStatus();

        if (!result.Changed)
        {
            await ShowMessageAsync(
                "Image transparency",
                result.Message ?? "The selected image reference transparency could not be updated.");
        }
    }

    private async Task OpenImageReferenceFolderFromManagerAsync(ImageReferenceItemViewModel reference)
    {
        if (string.IsNullOrWhiteSpace(reference.DirectoryPath))
        {
            await ShowMessageAsync(
                "Open image folder",
                "The selected image reference has no valid folder path.");
            return;
        }

        if (!Directory.Exists(reference.DirectoryPath))
        {
            await ShowMessageAsync(
                "Open image folder",
                "The selected image reference folder does not exist.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = reference.DirectoryPath,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Open image folder failed",
                exception.Message);
        }
    }

    private async Task<ImageFileSelection?> PickRasterImageAsync(string title)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { RasterImageFileType }
            });

        if (files.Count == 0)
        {
            return null;
        }

        string? filePath = files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            await ShowMessageAsync(
                title,
                "Only local image files are supported in this version.");
            return null;
        }

        using var bitmap = new Bitmap(filePath);

        return new ImageFileSelection(
            filePath,
            bitmap.PixelSize.Width,
            bitmap.PixelSize.Height);
    }

    private sealed record ImageFileSelection(
        string FilePath,
        int PixelWidth,
        int PixelHeight);

    private async void Save_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await SaveAsync(forceSaveAs: false);
    }

    private async void SaveAs_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await SaveAsync(forceSaveAs: true);
    }

    private async void ExportSvg_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            SvgExportOptions? options = await ShowSvgExportSettingsAsync();

            if (options is null)
            {
                return;
            }

            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export SVG",
                    SuggestedFileName = _viewModel.CurrentFileName == "Untitled"
                        ? "drawing.svg"
                        : System.IO.Path.ChangeExtension(_viewModel.CurrentFileName, ".svg"),
                    DefaultExtension = "svg",
                    FileTypeChoices = new[] { SvgFileType }
                });

            if (file is null)
            {
                return;
            }

            string? filePath = file.TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Export SVG",
                    "Only local files are supported in this version.");
                return;
            }

            _viewModel.ExportSvgToFile(
                filePath,
                options);

            RefreshStatus();
            CadCanvas.Focus();
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Export SVG failed",
                exception.Message);
        }
    }

    private async void ExportDxf_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export DXF",
                    SuggestedFileName = _viewModel.CurrentFileName == "Untitled"
                        ? "drawing.dxf"
                        : System.IO.Path.ChangeExtension(_viewModel.CurrentFileName, ".dxf"),
                    DefaultExtension = "dxf",
                    FileTypeChoices = new[] { DxfFileType }
                });

            if (file is null)
            {
                return;
            }

            string? filePath = file.TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Export DXF",
                    "Only local files are supported in this version.");
                return;
            }

            _viewModel.ExportDxfToFile(filePath);

            RefreshStatus();
            CadCanvas.Focus();
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Export DXF failed",
                exception.Message);
        }
    }

    private async void ExportPdf_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            PdfExportOptions? options = await ShowPdfExportSettingsAsync();

            if (options is null)
            {
                return;
            }

            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export PDF",
                    SuggestedFileName = _viewModel.CurrentFileName == "Untitled"
                        ? "drawing.pdf"
                        : System.IO.Path.ChangeExtension(_viewModel.CurrentFileName, ".pdf"),
                    DefaultExtension = "pdf",
                    FileTypeChoices = new[] { PdfFileType }
                });

            if (file is null)
            {
                return;
            }

            string? filePath = file.TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ShowMessageAsync(
                    "Export PDF",
                    "Only local files are supported in this version.");
                return;
            }

            _viewModel.ExportPdfToFile(
                filePath,
                options);

            RefreshStatus();
            CadCanvas.Focus();
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Export PDF failed",
                exception.Message);
        }
    }

    private async void About_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialog = new AboutWindow();

        await dialog.ShowDialog(this);

        CadCanvas.Focus();
    }





    private async Task<SvgExportOptions?> ShowSvgExportSettingsAsync()
    {
        string title = _viewModel.CurrentFileName == "Untitled"
            ? "OpenCad2D export"
            : _viewModel.CurrentFileName;

        var dialog = new SvgExportSettingsWindow(
            title,
            SvgExportOptions.Default);

        return await dialog.ShowDialog<SvgExportOptions?>(this);
    }

    private async Task<PdfExportOptions?> ShowPdfExportSettingsAsync()
    {
        var dialog = new PdfExportSettingsWindow(PdfExportOptions.Default);

        return await dialog.ShowDialog<PdfExportOptions?>(this);
    }

    private async Task ShowDxfImportReportAsync(DxfImportResult result)
    {
        var dialog = new DxfImportReportWindow(result);

        await dialog.ShowDialog(this);
    }

    private async Task<bool> SaveAsync(bool forceSaveAs)
    {
        try
        {
            string? filePath = _viewModel.CurrentFilePath;

            if (forceSaveAs || string.IsNullOrWhiteSpace(filePath))
            {
                IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "Save OpenCad2D drawing",
                        SuggestedFileName = _viewModel.CurrentFileName == "Untitled"
                            ? "drawing.opencad2d.json"
                            : _viewModel.CurrentFileName,
                        DefaultExtension = "opencad2d.json",
                        FileTypeChoices = new[] { OpenCad2DFileType }
                    });

                if (file is null)
                {
                    return false;
                }

                filePath = file.TryGetLocalPath();

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    await ShowMessageAsync(
                        "Save",
                        "Only local files are supported in this version.");
                    return false;
                }
            }

            _viewModel.SaveToFile(
                filePath,
                CadCanvas.GetViewportState());

            RefreshStatus();
            CadCanvas.Focus();

            return true;
        }
        catch (DocumentSaveException exception)
        {
            await ShowMessageAsync(
                "Save failed",
                exception.Message);

            return false;
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(
                "Save failed",
                exception.Message);

            return false;
        }
    }

    private async Task<bool> ConfirmProceedWithUnsavedChangesAsync()
    {
        if (!_viewModel.IsDirty)
        {
            return true;
        }

        SaveChangesChoice choice = await ShowSaveChangesDialogAsync();

        if (choice == SaveChangesChoice.Cancel)
        {
            return false;
        }

        if (choice == SaveChangesChoice.DontSave)
        {
            return true;
        }

        return await SaveAsync(forceSaveAs: false);
    }

    private async Task<SaveChangesChoice> ShowSaveChangesDialogAsync()
    {
        var dialog = new SaveChangesWindow(_viewModel.CurrentFileName);

        return await dialog.ShowDialog<SaveChangesChoice>(this);
    }

    private async void Window_Closing(
        object? sender,
        WindowClosingEventArgs e)
    {
        if (_closeConfirmed || !_viewModel.IsDirty)
        {
            return;
        }

        e.Cancel = true;

        if (!await ConfirmProceedWithUnsavedChangesAsync())
        {
            return;
        }

        _closeConfirmed = true;
        Close();
    }

    private async Task ShowMissingImageReferencesWarningIfNeededAsync()
    {
        int missingCount = _viewModel.MissingImageReferenceCount;

        if (missingCount <= 0)
        {
            return;
        }

        string noun = missingCount == 1
            ? "image reference is"
            : "image references are";

        await ShowMessageAsync(
            "Missing image references",
            $"{missingCount} external {noun} missing. Use Relink Missing to attach the correct PNG/JPG file while keeping the existing position, size and rotation.");
    }

    private async Task ShowMessageAsync(
        string title,
        string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(18),
                Spacing = 14,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        MinWidth = 80
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel &&
            panel.Children[1] is Button button)
        {
            button.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(this);
    }

    private void InitializeLayerComboBox()
    {
        LayerComboBox.ItemsSource = _viewModel.LayerNames;
        LayerComboBox.SelectedItem = _viewModel.CurrentLayer.Name;

        RefreshLayerControls();
    }

    private void InitializePolarTrackingComboBox()
    {
        PolarTrackingComboBox.ItemsSource = _viewModel.PolarTrackingOptions;
        PolarTrackingComboBox.SelectedItem = _viewModel.SelectedPolarTrackingOption;
    }

    private void RefreshLayerControls()
    {
        RefreshLayerVisibleCheckBox();
        RefreshLayerLockedCheckBox();
    }

    private void RefreshLayerVisibleCheckBox()
    {
        LayerVisibleCheckBox.IsChecked = _viewModel.CurrentLayer.IsVisible;
    }

    private void RefreshLayerLockedCheckBox()
    {
        LayerLockedCheckBox.IsChecked = _viewModel.CurrentLayer.IsLocked;
    }

    private void LayerVisibleCheckBox_Click(
        object? sender,
        RoutedEventArgs e)
    {
        bool isVisible = LayerVisibleCheckBox.IsChecked == true;

        _viewModel.SetCurrentLayerVisibility(isVisible);

        RefreshLayerControls();
        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
    }

    private void LayerLockedCheckBox_Click(
        object? sender,
        RoutedEventArgs e)
    {
        bool isLocked = LayerLockedCheckBox.IsChecked == true;

        _viewModel.SetCurrentLayerLocked(isLocked);

        RefreshLayerControls();
        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
    }

    private void AssignCurrentLayer_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.AssignSelectedEntitiesToCurrentLayer();

        RefreshLayerControls();
        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }


    private void LayerComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (LayerComboBox.SelectedItem is not string selectedLayerName)
        {
            return;
        }

        _viewModel.SetCurrentLayerByName(selectedLayerName);

        RefreshLayerControls();
        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void PolarTrackingComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (PolarTrackingComboBox.SelectedItem is not PolarTrackingOptionViewModel option)
        {
            return;
        }

        _viewModel.SetPolarTracking(option);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void Select_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Selection);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void SelectAll_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SelectAll();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void SelectLast_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SelectLast();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void Deselect_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.DeselectAll();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void Point_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Point);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Text_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Text);
        RefreshStatus();
        CadCanvas.Focus();
    }

    private void MultilineText_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.MultilineText);
        RefreshStatus();
        CadCanvas.Focus();
    }

    private void Line_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Line);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Rectangle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Rectangle);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void RectangleBySides_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.RectangleBySides);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Circle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Circle);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Ellipse_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Ellipse);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Arc_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Arc);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void ArcThreePoints_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.ArcThreePoints);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Polyline_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Polyline);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Spline_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Spline);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Polygon_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Polygon);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void NorthSymbol_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.NorthSymbol);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void ScaleBar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.ScaleBar);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }


    private void HorizontalDimension_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.HorizontalDimension);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void VerticalDimension_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.VerticalDimension);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void AlignedDimension_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.AlignedDimension);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void RadiusDimension_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.RadiusDimension);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void DiameterDimension_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.DiameterDimension);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void AngularDimension_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.AngularDimension);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void ZoomWindow_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.ZoomWindow);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Move_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Move);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Copy_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Copy);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Rotate_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Rotate);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Scale_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Scale);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Align_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Align);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void BreakAtPoint_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.BreakAtPoint);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void BreakBetweenPoints_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.BreakBetweenPoints);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Extend_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Extend);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Trim_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Trim);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Offset_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Offset);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void BoundaryFill_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.BoundaryFill);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Fillet_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Fillet);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Chamfer_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Chamfer);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Mirror_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Mirror);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }


    private void BeginPointPlacementSnapping(ToolResult result)
    {
        if (result.Kind != ToolResultKind.Started)
        {
            return;
        }

        CadCanvas.EnabledSnapsOverride = _viewModel.Workspace.Context.EnabledSnaps;
    }

    private void EndPointPlacementSnapping()
    {
        CadCanvas.EnabledSnapsOverride = null;
    }

    private async void CreateBlock_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var optionsWindow = new CreateBlockOptionsWindow();
        CreateBlockOptions? options = await optionsWindow
            .ShowDialog<CreateBlockOptions?>(this);

        if (options is null)
        {
            return;
        }

        ToolResult result = options.PickBasePointFromDrawing
            ? _viewModel.BeginCreateBlockBasePointPick(options)
            : _viewModel.CreateBlockFromSelection(options);

        if (options.PickBasePointFromDrawing)
        {
            BeginPointPlacementSnapping(result);
        }

        RefreshStatus();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (result.Message is not null && result.Kind != ToolResultKind.Completed && !options.PickBasePointFromDrawing)
        {
            await ShowMessageAsync(
                "Create Block",
                result.Message);
        }
    }

    private async void Library_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string libraryRootPath = ResolveLibraryRootPath();
        var catalog = new LibraryItemCatalog(new JsonDocumentSerializer());
        LibraryCatalogScanResult scanResult = catalog.Scan(libraryRootPath);
        var dialogViewModel = new LibraryWindowViewModel(scanResult);
        var dialog = new LibraryWindow(dialogViewModel);

        LibraryWindowResult? result = await dialog.ShowDialog<LibraryWindowResult?>(this);

        if (result is null)
        {
            CadCanvas.Focus();
            return;
        }

        ToolResult insertResult = _viewModel.BeginInsertLibraryItem(result.SelectedItem);

        BeginPointPlacementSnapping(insertResult);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (insertResult.Message is not null && insertResult.Kind != ToolResultKind.Started)
        {
            await ShowMessageAsync(
                "Library",
                insertResult.Message);
        }
    }

    private static string ResolveLibraryRootPath()
    {
        string currentDirectoryLibrary = Path.Combine(
            Environment.CurrentDirectory,
            "library");

        if (Directory.Exists(currentDirectoryLibrary))
        {
            return currentDirectoryLibrary;
        }

        return Path.Combine(
            AppContext.BaseDirectory,
            "library");
    }


    private async void InsertBlock_Click(
        object? sender,
        RoutedEventArgs e)
    {
        IReadOnlyList<OpenCad2D.Core.Blocks.BlockDefinition> definitions = _viewModel.BlockDefinitions;

        if (definitions.Count == 0)
        {
            await ShowMessageAsync(
                "Insert Block",
                "Create at least one block before inserting a block instance.");
            return;
        }

        var optionsWindow = new InsertBlockOptionsWindow(definitions);
        InsertBlockOptions? options = await optionsWindow
            .ShowDialog<InsertBlockOptions?>(this);

        if (options is null)
        {
            return;
        }

        ToolResult result = _viewModel.BeginInsertBlockPlacement(options);

        BeginPointPlacementSnapping(result);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (result.Message is not null && result.Kind != ToolResultKind.Started)
        {
            await ShowMessageAsync(
                "Insert Block",
                result.Message);
        }
    }

    private async void BlockManager_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialogViewModel = new BlockManagerWindowViewModel(
            _viewModel.Workspace.Document);

        var dialog = new BlockManagerWindow(dialogViewModel);

        BlockManagerResult? result = await dialog.ShowDialog<BlockManagerResult?>(this);

        if (result is null)
        {
            CadCanvas.Focus();
            return;
        }

        ToolResult updateResult = _viewModel.ApplyBlockDefinitionChanges(result.BlockDefinitions);

        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (updateResult.Message is not null && updateResult.Kind != ToolResultKind.Completed)
        {
            await ShowMessageAsync(
                "Block Manager",
                updateResult.Message);
            return;
        }

        if (result.Action != BlockManagerAction.InsertSelected ||
            result.SelectedBlockDefinitionId is null ||
            string.IsNullOrWhiteSpace(result.SelectedBlockName))
        {
            return;
        }

        ToolResult insertResult = _viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(
                result.SelectedBlockDefinitionId.Value,
                result.SelectedBlockName,
                1.0,
                0.0));

        BeginPointPlacementSnapping(insertResult);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (insertResult.Message is not null && insertResult.Kind != ToolResultKind.Started)
        {
            await ShowMessageAsync(
                "Block Manager",
                insertResult.Message);
        }
    }


    private async void EditBlock_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ToolResult result = _viewModel.BeginEditSelectedBlock();

        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (result.Message is not null && result.Kind != ToolResultKind.Completed)
        {
            await ShowMessageAsync(
                "Edit Block",
                result.Message);
        }
    }

    private async void SaveBlockEdit_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ToolResult result = _viewModel.SaveActiveBlockEdit();

        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (result.Message is not null && result.Kind != ToolResultKind.Completed)
        {
            await ShowMessageAsync(
                "Save Block Edit",
                result.Message);
        }
    }

    private async void CancelBlockEdit_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ToolResult result = _viewModel.CancelActiveBlockEdit();

        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();

        if (result.Message is not null && result.Kind != ToolResultKind.Completed)
        {
            await ShowMessageAsync(
                "Cancel Block Edit",
                result.Message);
        }
    }

    private void Explode_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Explode);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Join_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Join);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }


    private void AlignLeft_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.AlignSelectionLeft();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void AlignRight_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.AlignSelectionRight();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void AlignTop_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.AlignSelectionTop();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void AlignBottom_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.AlignSelectionBottom();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }



    private void DistributeHorizontal_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.DistributeSelectionHorizontally();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void DistributeVertical_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.DistributeSelectionVertically();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void BringToFront_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.BringSelectionToFront();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void SendToBack_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SendSelectionToBack();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void BringForward_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.BringSelectionForward();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void SendBackward_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SendSelectionBackward();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void Delete_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.DeleteSelection();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void MeasureDistance_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.MeasureDistance);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void MeasureEntity_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.MeasureEntity);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void MeasureAngle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.MeasureAngle);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void MeasureArea_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.MeasureArea);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Undo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.Undo();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void Redo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.Redo();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void ZoomExtents_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ToolResult result = CadCanvas.ZoomExtents();

        _viewModel.SetLastResult(result);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.Focus();
    }

    private void Properties_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.TogglePropertyPanel();

        RefreshStatus();

        CadCanvas.Focus();
    }

    private void PropertyPanelClose_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetPropertyPanelVisible(false);

        RefreshStatus();

        CadCanvas.Focus();
    }



    private async void Layers_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialogViewModel = new LayerManagerWindowViewModel(
            _viewModel.Workspace.Document,
            _viewModel.Workspace.CurrentLayerId);

        var dialog = new LayerManagerWindow(dialogViewModel);

        LayerManagerResult? result = await dialog.ShowDialog<LayerManagerResult?>(this);

        if (result is null)
        {
            CadCanvas.Focus();
            return;
        }

        ToolResult toolResult = _viewModel.ApplyLayerChanges(
            result.Layers,
            result.CurrentLayerId);

        InitializeLayerComboBox();
        RefreshLayerControls();
        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private async void LineFormats_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialogViewModel = new LineFormatManagerWindowViewModel(
            _viewModel.Workspace.Document);

        var dialog = new LineFormatManagerWindow(dialogViewModel);

        LineFormatManagerResult? result = await dialog.ShowDialog<LineFormatManagerResult?>(this);

        if (result is null)
        {
            CadCanvas.Focus();
            return;
        }

        ToolResult toolResult = _viewModel.ApplyLineFormatChanges(result.LineFormats);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private async void TextFormats_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialogViewModel = new TextFormatManagerWindowViewModel(
            _viewModel.Workspace.Document);

        var dialog = new TextFormatManagerWindow(dialogViewModel);

        TextFormatManagerResult? result = await dialog.ShowDialog<TextFormatManagerResult?>(this);

        if (result is null)
        {
            CadCanvas.Focus();
            return;
        }

        ToolResult toolResult = _viewModel.ApplyTextFormatChanges(result.TextFormats);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private async void DimensionStyles_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialogViewModel = new DimensionStyleManagerWindowViewModel(
            _viewModel.Workspace.Document);

        var dialog = new DimensionStyleManagerWindow(dialogViewModel);

        DimensionStyleManagerResult? result = await dialog.ShowDialog<DimensionStyleManagerResult?>(this);

        if (result is null)
        {
            CadCanvas.Focus();
            return;
        }

        ToolResult toolResult = _viewModel.ApplyDimensionStyleChanges(
            result.DimensionStyles,
            result.CurrentDimensionStyleId);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }





    private async void Grid_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialogViewModel = new GridSettingsWindowViewModel(
            _viewModel.Workspace.GridSettings);

        var dialog = new GridSettingsWindow(dialogViewModel);

        GridSettingsResult? result = await dialog.ShowDialog<GridSettingsResult?>(this);

        if (result is null)
        {
            CadCanvas.Focus();
            return;
        }

        ToolResult toolResult = _viewModel.ApplyGridSettings(result.GridSettings);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void Window_PreviewKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (!_viewModel.IsCommandHudVisible ||
            HasNonWhiteSpaceCommandInputText())
        {
            return;
        }

        if (e.Key == Key.Tab &&
            e.KeyModifiers == KeyModifiers.None)
        {
            CommitActiveLogicalHudField(confirm: false);
            MoveToNextLogicalHudField();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter &&
            e.KeyModifiers == KeyModifiers.None &&
            _activeLogicalHudFieldKind is not null)
        {
            if (!CommitActiveLogicalHudField(confirm: true))
            {
                ConfirmCommandHudOverrides();
            }

            ClearLogicalHudFieldInput();
            CadCanvas.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Back &&
            e.KeyModifiers == KeyModifiers.None &&
            _activeLogicalHudFieldKind is not null)
        {
            RemoveLastLogicalHudInputCharacter();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape &&
            _activeLogicalHudFieldKind is not null)
        {
            ClearLogicalHudFieldInput();
            _viewModel.CancelCommandHudInputOverrides();
            RefreshStatus();
            RefreshLogicalHudFieldVisuals();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
            CadCanvas.Focus();
            e.Handled = true;
        }
    }

    private void CommandInputTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (TryFocusFirstHudFieldKey(e))
        {
            return;
        }

        if (TryHandleCommandAutocompleteKey(e))
        {
            return;
        }

        if (TryHandleCommandHistoryNavigationKey(e))
        {
            return;
        }

        if (TryHandleAlignScaleConfirmationKey(e))
        {
            return;
        }

        if (TryHandleCommandOptionShortcutKey(e))
        {
            return;
        }

        if (TryHandlePolylineCompletionKey(e))
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            SubmitCommandInputText();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (HasCommandInputText())
            {
                ClearCommandInputText();
            }
            else
            {
                _viewModel.Escape();
                RefreshStatus();
                CadCanvas.ClearSnapMarker();
                CadCanvas.InvalidateVisual();
            }

            e.Handled = true;
        }
    }
    private void HudFieldTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (sender is not TextBox textBox ||
            textBox.DataContext is not CommandHudFieldViewModel field)
        {
            return;
        }

        if (e.Key == Key.Tab)
        {
            CommitHudFieldInput(
                textBox,
                field,
                confirm: false);
            FocusNextHudField(textBox);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            SubmitHudFieldInput(textBox, field);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            _viewModel.CancelCommandHudInputOverrides();
            ResetHudFieldTextBox(textBox, field);
            CadCanvas.Focus();
            e.Handled = true;
        }
    }

    private void HudFieldTextBox_GotFocus(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _activeKeyboardHudField = textBox;
            textBox.SelectAll();
        }
    }

    private void HudFieldTextBox_LostFocus(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            textBox.DataContext is not CommandHudFieldViewModel field)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            ResetHudFieldTextBox(textBox, field);
            return;
        }

        CommitHudFieldInput(
            textBox,
            field,
            confirm: false);
    }

    private void SubmitHudFieldInput(
        TextBox textBox,
        CommandHudFieldViewModel field)
    {
        CommitHudFieldInput(
            textBox,
            field,
            confirm: true);
    }

    private void CommitHudFieldInput(
        TextBox textBox,
        CommandHudFieldViewModel field,
        bool confirm)
    {
        if (!field.CanAcceptTypedOverride)
        {
            ResetHudFieldTextBox(textBox, field);
            return;
        }

        string input = (textBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            ResetHudFieldTextBox(textBox, field);
            return;
        }

        if (_viewModel.TryCommitCommandHudFieldInput(
                field.Kind,
                input,
                confirm,
                out _))
        {
            ClearCommandInputText();
            RefreshStatus();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();

            if (confirm)
            {
                CadCanvas.Focus();
            }

            return;
        }

        if (confirm)
        {
            SubmitCommandInputText(input);
        }
    }

    private static void ResetHudFieldTextBox(
        TextBox textBox,
        CommandHudFieldViewModel field)
    {
        textBox.Text = field.NumericValueText;
        textBox.CaretIndex = textBox.Text?.Length ?? 0;
    }


    private void Window_TextInput(
        object? sender,
        TextInputEventArgs e)
    {
        if (IsCommandInputSource(e.Source))
        {
            return;
        }

        string text = e.Text ?? string.Empty;

        if (!IsCommandInputText(text))
        {
            return;
        }

        if (TryRouteInitialNumericTextToHudField(text))
        {
            e.Handled = true;
            return;
        }

        AppendTextToCommandInput(text);

        e.Handled = true;
    }

    private bool TryRouteInitialNumericTextToHudField(string text)
    {
        if (!_viewModel.IsCommandHudVisible ||
            HasNonWhiteSpaceCommandInputText() ||
            !IsNumericHudText(text))
        {
            return false;
        }

        CommandHudFieldKind? targetKind = _activeLogicalHudFieldKind;

        if (targetKind is null ||
            !IsHudFieldKindCurrentlyAvailable(targetKind.Value))
        {
            targetKind = GetDefaultLogicalHudFieldKindForNumericText();
        }

        if (targetKind is null)
        {
            return false;
        }

        _activeLogicalHudFieldKind = targetKind.Value;
        _activeLogicalHudFieldText += text;

        if (_viewModel.TryCommitCommandHudFieldInput(
                targetKind.Value,
                _activeLogicalHudFieldText,
                confirm: false,
                out _))
        {
            RefreshStatus();
            RefreshLogicalHudFieldVisuals();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
        }

        return true;
    }

    private static bool IsPreferredInitialNumericHudField(CommandHudFieldKind kind)
    {
        return kind is
            CommandHudFieldKind.Distance or
            CommandHudFieldKind.Width or
            CommandHudFieldKind.Height or
            CommandHudFieldKind.Radius or
            CommandHudFieldKind.Factor or
            CommandHudFieldKind.Angle;
    }

    private static bool IsNumericHudText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (char character in text)
        {
            if (!char.IsDigit(character) &&
                character is not '.' and not ',' and not '-' and not '+')
            {
                return false;
            }
        }

        return true;
    }

    private bool TryFocusFirstHudFieldKey(KeyEventArgs e)
    {
        if (e.Key != Key.Tab ||
            HasNonWhiteSpaceCommandInputText() ||
            !_viewModel.IsCommandHudVisible)
        {
            return false;
        }

        CommitActiveLogicalHudField(confirm: false);
        MoveToNextLogicalHudField();
        e.Handled = true;
        return true;
    }

    private void RemoveLastLogicalHudInputCharacter()
    {
        if (string.IsNullOrEmpty(_activeLogicalHudFieldText))
        {
            return;
        }

        _activeLogicalHudFieldText = _activeLogicalHudFieldText[..^1];

        if (string.IsNullOrWhiteSpace(_activeLogicalHudFieldText))
        {
            _viewModel.CancelCommandHudInputOverrides();
            RefreshStatus();
            RefreshLogicalHudFieldVisuals();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
            return;
        }

        if (_activeLogicalHudFieldKind is not null &&
            _viewModel.TryCommitCommandHudFieldInput(
                _activeLogicalHudFieldKind.Value,
                _activeLogicalHudFieldText,
                confirm: false,
                out _))
        {
            RefreshStatus();
            RefreshLogicalHudFieldVisuals();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
        }
    }

    private bool CommitActiveLogicalHudField(bool confirm)
    {
        if (_activeLogicalHudFieldKind is null ||
            string.IsNullOrWhiteSpace(_activeLogicalHudFieldText))
        {
            return false;
        }

        CommandHudFieldKind fieldKind = _activeLogicalHudFieldKind.Value;
        string input = _activeLogicalHudFieldText;
        bool handled = _viewModel.TryCommitCommandHudFieldInput(
            fieldKind,
            input,
            confirm,
            out _);

        if (handled)
        {
            RefreshStatus();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
        }

        if (confirm)
        {
            ClearLogicalHudFieldInput();
        }
        else
        {
            _activeLogicalHudFieldText = string.Empty;
        }

        return handled;
    }

    private void ConfirmCommandHudOverrides()
    {
        if (_viewModel.TryConfirmCommandHudInputOverrides(out _))
        {
            RefreshStatus();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
        }
    }

    private void MoveToNextLogicalHudField()
    {
        List<CommandHudFieldKind> availableKinds = GetAvailableHudFieldKinds().ToList();

        if (availableKinds.Count == 0)
        {
            ClearLogicalHudFieldInput();
            CadCanvas.Focus();
            return;
        }

        int currentIndex = _activeLogicalHudFieldKind is null
            ? -1
            : availableKinds.IndexOf(_activeLogicalHudFieldKind.Value);

        int nextIndex = currentIndex < 0 || currentIndex >= availableKinds.Count - 1
            ? 0
            : currentIndex + 1;

        _activeLogicalHudFieldKind = availableKinds[nextIndex];
        _activeLogicalHudFieldText = string.Empty;
        CadCanvas.Focus();
        RefreshLogicalHudFieldVisuals();
        Dispatcher.UIThread.Post(RefreshLogicalHudFieldVisuals, DispatcherPriority.Background);
    }

    private CommandHudFieldKind? GetDefaultLogicalHudFieldKindForNumericText()
    {
        if (_activeLogicalHudFieldKind is not null &&
            IsHudFieldKindCurrentlyAvailable(_activeLogicalHudFieldKind.Value))
        {
            return _activeLogicalHudFieldKind.Value;
        }

        foreach (CommandHudFieldKind kind in GetAvailableHudFieldKinds())
        {
            if (IsPreferredInitialNumericHudField(kind))
            {
                return kind;
            }
        }

        return null;
    }

    private bool IsHudFieldKindCurrentlyAvailable(CommandHudFieldKind kind)
    {
        return GetAvailableHudFieldKinds().Contains(kind);
    }

    private IEnumerable<CommandHudFieldKind> GetAvailableHudFieldKinds()
    {
        return _viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .Distinct();
    }

    private void ClearLogicalHudFieldInput()
    {
        _activeLogicalHudFieldKind = null;
        _activeLogicalHudFieldText = string.Empty;
        _activeKeyboardHudField = null;
        RefreshLogicalHudFieldVisuals();
        Dispatcher.UIThread.Post(RefreshLogicalHudFieldVisuals, DispatcherPriority.Background);
    }

    private void RefreshLogicalHudFieldVisuals()
    {
        if (HudFieldsItemsControl is null)
        {
            return;
        }

        foreach (TextBox textBox in HudFieldsItemsControl.GetVisualDescendants().OfType<TextBox>())
        {
            if (textBox.DataContext is not CommandHudFieldViewModel field)
            {
                continue;
            }

            bool isActive = _activeLogicalHudFieldKind == field.Kind;

            textBox.BorderBrush = isActive
                ? new SolidColorBrush(Color.Parse("#FFD700"))
                : new SolidColorBrush(Color.Parse("#444444"));

            textBox.BorderThickness = isActive
                ? new Thickness(2)
                : new Thickness(1);

            textBox.Background = isActive
                ? new SolidColorBrush(Color.Parse("#241E04"))
                : new SolidColorBrush(Color.Parse("#111111"));

            textBox.Foreground = isActive
                ? Brushes.White
                : new SolidColorBrush(Color.Parse("#FFD700"));

            if (isActive)
            {
                textBox.Text = string.IsNullOrEmpty(_activeLogicalHudFieldText)
                    ? field.NumericValueText
                    : _activeLogicalHudFieldText;
            }
            else
            {
                textBox.Text = field.NumericValueText;
            }

            textBox.CaretIndex = textBox.Text?.Length ?? 0;
        }
    }

    private bool FocusFirstHudField()
    {
        MoveToNextLogicalHudField();
        return _activeLogicalHudFieldKind is not null;
    }

    private void FocusNextHudField(TextBox currentField)
    {
        MoveToNextLogicalHudField();
    }

    private bool IsCurrentHudFieldTextBox(TextBox candidate)
    {
        return false;
    }

    private IEnumerable<TextBox> GetHudFieldTextBoxes()
    {
        return Enumerable.Empty<TextBox>();
    }

    private bool TryHandleCommandAutocompleteKey(KeyEventArgs e)
    {
        if (e.Key != Key.Tab)
        {
            return false;
        }

        string currentText = GetCommandInputText();

        if (string.IsNullOrWhiteSpace(currentText))
        {
            return false;
        }

        string? suggestion = _viewModel.GetCommandAutocompleteSuggestion(currentText);

        if (string.IsNullOrWhiteSpace(suggestion))
        {
            return false;
        }

        SetCommandInputText(suggestion);
        CadCanvas.Focus();
        e.Handled = true;
        return true;
    }

    private bool TryHandleCommandHistoryNavigationKey(KeyEventArgs e)
    {
        if (e.Key != Key.Up && e.Key != Key.Down)
        {
            return false;
        }

        string commandText = e.Key == Key.Up
            ? _viewModel.NavigateCommandHistoryPrevious()
            : _viewModel.NavigateCommandHistoryNext();

        SetCommandInputText(commandText);
        CadCanvas.Focus();
        e.Handled = true;
        return true;
    }

    private void AppendTextToCommandInput(string text)
    {
        SetCommandInputText(GetCommandInputText() + text);
        CadCanvas.Focus();
    }

    private void RemoveLastCommandInputCharacter()
    {
        string text = GetCommandInputText();

        if (text.Length == 0)
        {
            return;
        }

        SetCommandInputText(text[..^1]);
        CadCanvas.Focus();
    }

    private void ClearCommandInputText()
    {
        SetCommandInputText(string.Empty);
    }

    private bool HasCommandInputText()
    {
        return GetCommandInputText().Length > 0;
    }

    private bool HasNonWhiteSpaceCommandInputText()
    {
        return !string.IsNullOrWhiteSpace(GetCommandInputText());
    }

    private string GetCommandInputText()
    {
        return _commandInputBuffer;
    }

    private void SubmitCommandInputText()
    {
        SubmitCommandInputText(GetCommandInputText());
    }

    private void SubmitCommandInputText(string? text)
    {
        ToolResult result = _viewModel.SubmitCommandInput(text);
        _viewModel.SetLastResult(result);

        ClearCommandInputText();
        ClearLogicalHudFieldInput();

        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private static bool IsCommandInputSource(object? source)
    {
        return source is TextBox;
    }

    private bool TryHandleAlignScaleConfirmationKey(KeyEventArgs e)
    {
        if (HasNonWhiteSpaceCommandInputText() ||
            _viewModel.Workspace.ToolController.ActiveTool is not AlignTool alignTool ||
            alignTool.State != AlignToolState.WaitingForScaleConfirmation)
        {
            return false;
        }

        if (e.Key == Key.Y)
        {
            SubmitCommandInputText("Y");
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.N)
        {
            SubmitCommandInputText("N");
            e.Handled = true;
            return true;
        }

        return false;
    }

    private bool TryHandleCommandOptionShortcutKey(KeyEventArgs e)
    {
        if (HasNonWhiteSpaceCommandInputText() ||
            e.KeyModifiers != KeyModifiers.None ||
            _viewModel.Workspace.ToolController.ActiveTool is not ICommandDrivenTool commandDrivenTool)
        {
            return false;
        }

        string shortcut = e.Key.ToString();
        CommandPromptState promptState = commandDrivenTool.GetPromptState(_viewModel.Workspace.Context);
        CommandOption? option = promptState.Options
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Shortcut, shortcut, StringComparison.OrdinalIgnoreCase));

        if (option is null)
        {
            return false;
        }

        SubmitCommandInputText(option.Keyword);
        e.Handled = true;
        return true;
    }

    private bool TryHandlePolylineCompletionKey(KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            HasNonWhiteSpaceCommandInputText() ||
            _viewModel.Workspace.ToolController.ActiveTool is not PolylineTool)
        {
            return false;
        }

        SubmitCommandInputText(string.Empty);
        e.Handled = true;
        return true;
    }

    private void FocusCommandInputIfAlignScaleConfirmation()
    {
        if (_viewModel.Workspace.ToolController.ActiveTool is AlignTool alignTool &&
            alignTool.State == AlignToolState.WaitingForScaleConfirmation)
        {
            ClearLogicalHudFieldInput();
            CadCanvas.Focus();
        }
    }

    private void SetCommandInputText(string text)
    {
        _commandInputBuffer = text;
    }

    private static bool IsCommandInputText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (char character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                continue;
            }

            if (character is ',' or '.' or '-' or '+' or '@' or '_' or ' ')
            {
                continue;
            }

            return false;
        }

        return true;
    }


    private void CadCanvas_RepeatLastCommandRequested(
        object? sender,
        EventArgs e)
    {
        ToolResult result = _viewModel.RepeatLastCommandFromCanvas();
        _viewModel.SetLastResult(result);

        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private async void CadCanvas_WorkspaceChanged(
        object? sender,
        CadCanvasWorkspaceChangedEventArgs e)
    {
        _viewModel.SetMousePosition(e.MousePosition);
        _viewModel.SetCurrentSnapCandidate(e.SnapCandidate);
        _viewModel.SetHudScreenPosition(e.PointerScreenPosition);
        UpdateCommandHudPosition();

        if (_viewModel.IsCreateBlockBasePointPickPending)
        {
            ToolResult createBlockResult;

            if (e.Result.Kind == ToolResultKind.Cancelled)
            {
                createBlockResult = _viewModel.CancelCreateBlockBasePointPick();
                EndPointPlacementSnapping();
            }
            else if (e.IsPointerPressed)
            {
                Point2D basePoint = e.SnapCandidate?.Point ?? e.MousePosition;
                createBlockResult = _viewModel.CommitCreateBlockBasePointPick(basePoint);
                EndPointPlacementSnapping();

                CadCanvas.ClearSnapMarker();
                CadCanvas.InvalidateVisual();
            }
            else
            {
                createBlockResult = ToolResult.Started("Create block: specify base point.");
            }

            _viewModel.SetLastResult(createBlockResult);
        }
        else if (_viewModel.IsBlockInsertionPending)
        {
            ToolResult insertBlockResult;

            if (e.Result.Kind == ToolResultKind.Cancelled)
            {
                insertBlockResult = _viewModel.CancelPendingBlockInsertion();
                EndPointPlacementSnapping();
            }
            else if (e.IsPointerPressed)
            {
                Point2D insertionPoint = e.SnapCandidate?.Point ?? e.MousePosition;
                insertBlockResult = _viewModel.CommitPendingBlockInsertion(insertionPoint);
                EndPointPlacementSnapping();

                CadCanvas.ClearSnapMarker();
                CadCanvas.InvalidateVisual();
            }
            else
            {
                insertBlockResult = ToolResult.Started("Insert block: specify insertion point.");
            }

            _viewModel.SetLastResult(insertBlockResult);
        }
        else if (_viewModel.IsLibraryInsertionPending)
        {
            ToolResult libraryInsertResult;

            if (e.Result.Kind == ToolResultKind.Cancelled)
            {
                libraryInsertResult = _viewModel.CancelPendingLibraryInsertion();
                EndPointPlacementSnapping();
            }
            else if (e.IsPointerPressed)
            {
                Point2D insertionPoint = e.SnapCandidate?.Point ?? e.MousePosition;
                libraryInsertResult = _viewModel.CommitPendingLibraryInsertion(insertionPoint);
                EndPointPlacementSnapping();

                CadCanvas.ClearSnapMarker();
                CadCanvas.InvalidateVisual();
            }
            else
            {
                libraryInsertResult = ToolResult.Started("Library: specify insertion point.");
            }

            _viewModel.SetLastResult(libraryInsertResult);
        }
        else if (_viewModel.IsImportDrawingPlacementPending)
        {
            ToolResult importResult;

            if (e.Result.Kind == ToolResultKind.Cancelled)
            {
                importResult = _viewModel.CancelPendingImportDrawing();
                EndPointPlacementSnapping();
            }
            else if (e.IsPointerPressed)
            {
                Point2D insertionPoint = e.SnapCandidate?.Point ?? e.MousePosition;
                importResult = _viewModel.CommitPendingImportDrawing(insertionPoint);
                EndPointPlacementSnapping();

                CadCanvas.ClearSnapMarker();
                CadCanvas.InvalidateVisual();

                await ShowMissingImageReferencesWarningIfNeededAsync();
            }
            else
            {
                importResult = ToolResult.Started("Import drawing: specify insertion point.");
            }

            _viewModel.SetLastResult(importResult);
        }
        else
        {
            _viewModel.SetLastResult(e.Result);
        }

        if (e.IsPointerPressed)
        {
            ClearLogicalHudFieldInput();
            _viewModel.ClearCommandHudInputOverridesForNextInput();
        }

        _viewModel.NotifyDocumentStateChanged();

        RefreshStatus();
        FocusCommandInputIfAlignScaleConfirmation();
    }

    private void RefreshStatus()
    {
        Title = _viewModel.TitleText;

        PropertiesButton.Content = _viewModel.IsPropertyPanelVisible
            ? "Props ✓"
            : "Props";

        RefreshActiveToolUi();
        UpdateCommandHudPosition();
    }

    private void UpdateCommandHudPosition()
    {
        if (!_viewModel.IsCommandHudVisible || _viewModel.HudScreenPosition is not { } pointerPosition)
        {
            CommandHudPanel.IsVisible = false;
            return;
        }

        CommandHudPanel.IsVisible = true;
        UpdateCommandHudIcon();
        RefreshLogicalHudFieldVisuals();
        Dispatcher.UIThread.Post(RefreshLogicalHudFieldVisuals, DispatcherPriority.Background);

        const double offsetX = 20;
        const double offsetY = 15;
        const double margin = 8;

        double hudWidth = CommandHudPanel.Bounds.Width > 0
            ? CommandHudPanel.Bounds.Width
            : 240;

        double hudHeight = CommandHudPanel.Bounds.Height > 0
            ? CommandHudPanel.Bounds.Height
            : 130;

        double maxX = Math.Max(margin, CommandHudOverlay.Bounds.Width - hudWidth - margin);
        double maxY = Math.Max(margin, CommandHudOverlay.Bounds.Height - hudHeight - margin);

        double x = Math.Clamp(pointerPosition.X + offsetX, margin, maxX);
        double y = Math.Clamp(pointerPosition.Y + offsetY, margin, maxY);

        Canvas.SetLeft(CommandHudPanel, x);
        Canvas.SetTop(CommandHudPanel, y);
    }


    private void UpdateCommandHudIcon()
    {
        string? resourceKey = GetCommandHudIconResourceKey(_viewModel.ActiveToolName);

        if (resourceKey is null ||
            !this.TryGetResource(resourceKey, null, out object? resource) ||
            resource is not Avalonia.Media.Geometry geometry)
        {
            HudToolIcon.Data = null;
            HudToolIcon.IsVisible = false;
            return;
        }

        HudToolIcon.Data = geometry;
        HudToolIcon.IsVisible = true;
    }

    private static string? GetCommandHudIconResourceKey(string activeToolName)
    {
        return activeToolName switch
        {
            "Zoom Window" => "IconZoomWindow",
            "Point" => "IconPoint",
            "Text" => "IconText",
            "MText" => "IconMText",
            "Line" => "IconLine",
            "Rectangle" => "IconRectangle",
            "Rect Sides" => "IconRectSides",
            "Circle" => "IconCircle",
            "Ellipse" => "IconEllipse",
            "Arc" => "IconArc",
            "Arc 3P" => "IconArc3P",
            "Polyline" => "IconPolyline",
            "Spline" => "IconSpline",
            "Polygon" => "IconPolygon",
            "North Symbol" => "IconPoint",
            "Metric Scale Bar" => "IconDistance",
            "Horizontal Dim" => "IconHorizontalDim",
            "Vertical Dim" => "IconVerticalDim",
            "Aligned Dim" => "IconAlignedDim",
            "Radius Dim" => "IconRadiusDim",
            "Diameter Dim" => "IconDiameterDim",
            "Angular Dim" => "IconAngularDim",
            "Move" => "IconMove",
            "Copy" => "IconCopy",
            "Rotate" => "IconRotate",
            "Scale" => "IconScale",
            "Align" => "IconAlign",
            "Break Point" => "IconBreakPt",
            "Break Segment" => "IconBreakSeg",
            "Extend" => "IconExtend",
            "Trim" => "IconTrim",
            "Offset" => "IconOffset",
            "Boundary Fill" => "IconBoundaryFill",
            "Fillet" => "IconFillet",
            "Chamfer" => "IconChamfer",
            "Mirror" => "IconMirror",
            "Explode" => "IconExplode",
            "Join" => "IconJoin",
            "Delete" => "IconDelete",
            "Distance" => "IconDistance",
            "Entity" => "IconSelect",
            "Angle" => "IconAngle",
            "Area" => "IconArea",
            _ => null
        };
    }

    private void RefreshSnapControls()
    {
        SnapEndpointCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Endpoint);
        SnapMidpointCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Midpoint);
        SnapCenterCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Center);
        SnapQuadrantCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Quadrant);
        SnapIntersectionCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Intersection);
        SnapPerpendicularCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Perpendicular);
        SnapTangentCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Tangent);
        SnapGridCheckBox.IsChecked = _viewModel.IsSnapEnabled(SnapKind.Grid);
        OrthoCheckBox.IsChecked = _viewModel.IsOrthoEnabled;
    }

    private void SnapEndpoint_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Endpoint,
            SnapEndpointCheckBox.IsChecked == true);
    }

    private void SnapMidpoint_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Midpoint,
            SnapMidpointCheckBox.IsChecked == true);
    }

    private void SnapCenter_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Center,
            SnapCenterCheckBox.IsChecked == true);
    }

    private void SnapQuadrant_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Quadrant,
            SnapQuadrantCheckBox.IsChecked == true);
    }

    private void SnapIntersection_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Intersection,
            SnapIntersectionCheckBox.IsChecked == true);
    }

    private void SnapGrid_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Grid,
            SnapGridCheckBox.IsChecked == true);
    }

    private void SnapPerpendicular_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Perpendicular,
            SnapPerpendicularCheckBox.IsChecked == true);
    }

    private void SnapTangent_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Tangent,
            SnapTangentCheckBox.IsChecked == true);
    }

    private void Ortho_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetOrthoEnabled(OrthoCheckBox.IsChecked == true);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
    }

    private void SetSnapFromCheckBox(
        SnapKind snapKind,
        bool isEnabled)
    {
        _viewModel.SetSnapEnabled(
            snapKind,
            isEnabled);

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
    }

    private void RefreshActiveToolUi()
    {
        string activeToolName = _viewModel.ActiveToolName;

        SetActiveToolButton(
            SelectButton,
            activeToolName.Equals("Select", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("Selection", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            PointButton,
            activeToolName.Equals("Point", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            TextButton,
            activeToolName.Equals("Text", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MultilineTextButton,
            activeToolName.Equals("Multiline Text", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("MultilineText", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            LineButton,
            activeToolName.Equals("Line", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            RectangleButton,
            activeToolName.Equals("Rectangle", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            RectangleBySidesButton,
            activeToolName.Equals("Rectangle Sides", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            CircleButton,
            activeToolName.Equals("Circle", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            EllipseButton,
            activeToolName.Equals("Ellipse", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ArcButton,
            activeToolName.Equals("Arc", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ArcThreePointsButton,
            activeToolName.Equals("Arc 3P", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            PolylineButton,
            activeToolName.Equals("Polyline", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            PolygonButton,
            activeToolName.Equals("Polygon", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            NorthSymbolButton,
            activeToolName.Equals("North Symbol", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("NorthSymbol", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ScaleBarButton,
            activeToolName.Equals("Metric Scale Bar", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("Scale Bar", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("ScaleBar", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            HorizontalDimensionButton,
            activeToolName.Equals("Horizontal Dimension", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("HorizontalDimension", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            VerticalDimensionButton,
            activeToolName.Equals("Vertical Dimension", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("VerticalDimension", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            AlignedDimensionButton,
            activeToolName.Equals("Aligned Dimension", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("AlignedDimension", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            RadiusDimensionButton,
            activeToolName.Equals("Radius Dimension", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("RadiusDimension", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            DiameterDimensionButton,
            activeToolName.Equals("Diameter Dimension", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("DiameterDimension", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            AngularDimensionButton,
            activeToolName.Equals("Angular Dimension", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("AngularDimension", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ZoomWindowButton,
            activeToolName.Equals("ZoomWindow", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("Zoom Window", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MoveButton,
            activeToolName.Equals("Move", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            CopyButton,
            activeToolName.Equals("Copy", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            RotateButton,
            activeToolName.Equals("Rotate", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ScaleButton,
            activeToolName.Equals("Scale", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            AlignButton,
            activeToolName.Equals("Align", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            BreakAtPointButton,
            activeToolName.Equals("BreakAtPoint", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("Break Point", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            BreakBetweenPointsButton,
            activeToolName.Equals("BreakBetweenPoints", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("Break Segment", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ExtendButton,
            activeToolName.Equals("Extend", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            TrimButton,
            activeToolName.Equals("Trim", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            OffsetButton,
            activeToolName.Equals("Offset", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            BoundaryFillButton,
            activeToolName.Equals("Boundary Fill", StringComparison.OrdinalIgnoreCase) ||
            activeToolName.Equals("BoundaryFill", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            FilletButton,
            activeToolName.Equals("Fillet", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ChamferButton,
            activeToolName.Equals("Chamfer", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MirrorButton,
            activeToolName.Equals("Mirror", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MeasureDistanceButton,
            activeToolName.Equals("Measure Distance", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MeasureEntityButton,
            activeToolName.Equals("Measure Entity", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MeasureAngleButton,
            activeToolName.Equals("Measure Angle", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MeasureAreaButton,
            activeToolName.Equals("Measure Area", StringComparison.OrdinalIgnoreCase));

    }

    private static void SetActiveToolButton(
        Button button,
        bool isActive)
    {
        const string activeClassName = "active-tool";

        if (isActive)
        {
            if (!button.Classes.Contains(activeClassName))
            {
                button.Classes.Add(activeClassName);
            }

            return;
        }

        button.Classes.Remove(activeClassName);
    }
}
