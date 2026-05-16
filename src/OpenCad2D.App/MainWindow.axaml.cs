using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
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
using OpenCad2D.App.ViewModels.PolarTracking;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Export.Pdf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Core.Layers;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Measurements;
using System;

namespace OpenCad2D.App;

public partial class MainWindow : Window
{
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

        InitializeLayerComboBox();
        InitializePolarTrackingComboBox();
        RefreshLayerControls();
        RefreshStatus();

        Closing += Window_Closing;
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
        InitializeLayerComboBox();
        InitializePolarTrackingComboBox();
        RefreshLayerControls();
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
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
            InitializeLayerComboBox();
            InitializePolarTrackingComboBox();
            RefreshLayerControls();
            RefreshStatus();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
            CadCanvas.Focus();
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
            InitializeLayerComboBox();
            InitializePolarTrackingComboBox();
            RefreshLayerControls();
            RefreshStatus();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
            CadCanvas.Focus();

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

    private void CommandInputTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (TryHandleAlignScaleConfirmationKey(e))
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
            if (!string.IsNullOrEmpty(CommandInputTextBox.Text))
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

    private void Window_TextInput(
        object? sender,
        TextInputEventArgs e)
    {
        if (ReferenceEquals(e.Source, CommandInputTextBox))
        {
            return;
        }

        string text = e.Text ?? string.Empty;

        if (!IsCommandInputText(text))
        {
            return;
        }

        AppendTextToCommandInput(text);

        e.Handled = true;
    }

    private void Window_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (TryHandleFileShortcut(e))
        {
            return;
        }

        if (ReferenceEquals(e.Source, CommandInputTextBox))
        {
            return;
        }

        if (e.Source is TextBox)
        {
            return;
        }

        if (TryHandleAlignScaleConfirmationKey(e))
        {
            return;
        }

        if (TryHandlePolylineCompletionKey(e))
        {
            return;
        }

        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(CommandInputTextBox.Text))
        {
            SubmitCommandInputText();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Back && !string.IsNullOrEmpty(CommandInputTextBox.Text))
        {
            RemoveLastCommandInputCharacter();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(CommandInputTextBox.Text))
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


    private bool TryHandleFileShortcut(KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return false;
        }

        if (e.Key == Key.N)
        {
            New_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.O)
        {
            Open_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            SaveAs_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.S)
        {
            Save_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return true;
        }

        return false;
    }


    private void CadCanvas_RepeatLastCommandRequested(
        object? sender,
        EventArgs e)
    {
        _viewModel.RepeatLastCommandFromCanvas();
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void SubmitCommandInputText()
    {
        string input = CommandInputTextBox.Text ?? string.Empty;

        _viewModel.SubmitCommandInput(input);

        ClearCommandInputText();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();

        if (!FocusCommandInputIfAlignScaleConfirmation())
        {
            CadCanvas.Focus();
        }
    }

    private bool TryHandlePolylineCompletionKey(KeyEventArgs e)
    {
        if (_viewModel.Workspace.ToolController.ActiveTool is not PolylineTool polylineTool ||
            polylineTool.State != PolylineToolState.CollectingVertices ||
            !string.IsNullOrWhiteSpace(CommandInputTextBox.Text))
        {
            return false;
        }

        if (e.Key == Key.Enter)
        {
            CompletePolyline(isClosed: false);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.C)
        {
            CompletePolyline(isClosed: true);
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void CompletePolyline(bool isClosed)
    {
        if (_viewModel.Workspace.ToolController.ActiveTool is not PolylineTool polylineTool)
        {
            return;
        }

        ToolResult result = isClosed
            ? polylineTool.CompleteClosed(_viewModel.Workspace.Context)
            : polylineTool.CompleteOpen(_viewModel.Workspace.Context);

        ClearCommandInputText();

        _viewModel.SetLastResult(result);
        _viewModel.NotifyDocumentStateChanged();
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private bool TryHandleAlignScaleConfirmationKey(KeyEventArgs e)
    {
        if (_viewModel.Workspace.ToolController.ActiveTool is not AlignTool alignTool ||
            alignTool.State != AlignToolState.WaitingForScaleConfirmation ||
            !string.IsNullOrWhiteSpace(CommandInputTextBox.Text))
        {
            return false;
        }

        if (e.Key == Key.Enter || e.Key == Key.N)
        {
            ConfirmAlignScale(applyScale: false);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Y)
        {
            ConfirmAlignScale(applyScale: true);
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void ConfirmAlignScale(bool applyScale)
    {
        if (_viewModel.Workspace.ToolController.ActiveTool is not AlignTool alignTool)
        {
            return;
        }

        ToolResult result = applyScale
            ? alignTool.ConfirmWithScale(_viewModel.Workspace.Context)
            : alignTool.ConfirmWithoutScale(_viewModel.Workspace.Context);

        ClearCommandInputText();

        _viewModel.SetLastResult(result);
        _viewModel.NotifyDocumentStateChanged();
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private bool FocusCommandInputIfAlignScaleConfirmation()
    {
        if (_viewModel.Workspace.ToolController.ActiveTool is not AlignTool alignTool ||
            alignTool.State != AlignToolState.WaitingForScaleConfirmation)
        {
            return false;
        }

        ClearCommandInputText();
        CommandInputTextBox.Focus();
        return true;
    }

    private void AppendTextToCommandInput(string text)
    {
        CommandInputTextBox.Text = (CommandInputTextBox.Text ?? string.Empty) + text;
        CommandInputTextBox.CaretIndex = CommandInputTextBox.Text.Length;
        CommandInputTextBox.Focus();
    }

    private void RemoveLastCommandInputCharacter()
    {
        string text = CommandInputTextBox.Text ?? string.Empty;

        if (text.Length == 0)
        {
            return;
        }

        CommandInputTextBox.Text = text[..^1];
        CommandInputTextBox.CaretIndex = CommandInputTextBox.Text.Length;
        CommandInputTextBox.Focus();
    }

    private void ClearCommandInputText()
    {
        CommandInputTextBox.Text = string.Empty;
        CommandInputTextBox.CaretIndex = 0;
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

    private void CadCanvas_WorkspaceChanged(
        object? sender,
        CadCanvasWorkspaceChangedEventArgs e)
    {
        _viewModel.SetMousePosition(e.MousePosition);
        _viewModel.SetLastResult(e.Result);
        _viewModel.SetCurrentSnapCandidate(e.SnapCandidate);
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
            ArcButton,
            activeToolName.Equals("Arc", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            ArcThreePointsButton,
            activeToolName.Equals("Arc 3P", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            PolylineButton,
            activeToolName.Equals("Polyline", StringComparison.OrdinalIgnoreCase));


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

        ActiveCommandTextBlock.Text = $"Comando attivo: {activeToolName}";
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