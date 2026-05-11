using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using OpenCad2D.Persistence;
using Avalonia.Platform.Storage;
using Avalonia.Layout;
using OpenCad2D.App.Controls;
using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.Layers;
using OpenCad2D.App.ViewModels.Grid;
using OpenCad2D.App.ViewModels.LineFormats;
using OpenCad2D.App.ViewModels.PolarTracking;
using OpenCad2D.Core.Layers;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
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

    private readonly MainWindowViewModel _viewModel;
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel();

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

            _viewModel.ExportSvgToFile(filePath);

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

    private enum SaveChangesChoice
    {
        Cancel,
        Save,
        DontSave
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
        var saveButton = new Button
        {
            Content = "Save",
            MinWidth = 92
        };

        var dontSaveButton = new Button
        {
            Content = "Don't Save",
            MinWidth = 92
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 92
        };

        var dialog = new Window
        {
            Title = "Save changes?",
            Width = 460,
            Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(18),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Save changes to '{_viewModel.CurrentFileName}' before continuing?",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children =
                        {
                            saveButton,
                            dontSaveButton,
                            cancelButton
                        }
                    }
                }
            }
        };

        saveButton.Click += (_, _) => dialog.Close(SaveChangesChoice.Save);
        dontSaveButton.Click += (_, _) => dialog.Close(SaveChangesChoice.DontSave);
        cancelButton.Click += (_, _) => dialog.Close(SaveChangesChoice.Cancel);

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

        if (e.Key == Key.Escape && !string.IsNullOrEmpty(CommandInputTextBox.Text))
        {
            ClearCommandInputText();
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

        if (e.Key == Key.Escape && !string.IsNullOrEmpty(CommandInputTextBox.Text))
        {
            ClearCommandInputText();
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
            if (char.IsDigit(character))
            {
                continue;
            }

            if (character is ',' or '.' or '-' or '+' or '@' or ' ')
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