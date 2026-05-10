using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using OpenCad2D.App.Controls;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Layers;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Persistence;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace OpenCad2D.App;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType OpenCad2DFileType = new("OpenCad2D drawing")
    {
        Patterns = new[] { "*.opencad2d.json" },
        MimeTypes = new[] { "application/json" }
    };

    private readonly MainWindowViewModel _viewModel;
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel();

        DataContext = _viewModel;

        InitializeLayerComboBox();
        RefreshLayerControls();
        RefreshGridControls();
        RefreshStatus();

        Closing += Window_Closing;
    }

    private void InitializeLayerComboBox()
    {
        LayerComboBox.ItemsSource = _viewModel.LayerNames;
        LayerComboBox.SelectedItem = _viewModel.CurrentLayer.Name;

        RefreshLayerControls();
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

    private void RefreshGridControls()
    {
        GridVisibleCheckBox.IsChecked = _viewModel.IsGridVisible;
        GridMinorStepTextBox.Text = FormatGridValue(_viewModel.GridMinorStep);
        GridMajorStepTextBox.Text = FormatGridValue(_viewModel.GridMajorStep);
        GridMinScreenSpacingTextBox.Text = FormatGridValue(_viewModel.GridMinimumScreenSpacing);
        GridMaxScreenSpacingTextBox.Text = FormatGridValue(_viewModel.GridMaximumScreenSpacing);
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
        RefreshLayerControls();
        RefreshGridControls();
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
            RefreshLayerControls();
            RefreshGridControls();
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

    private void Circle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Circle);

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


    private void CommandInputTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
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

        if (e.Source is TextBox)
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

    private async void Window_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (ReferenceEquals(e.Source, CommandInputTextBox))
        {
            return;
        }

        if (e.Source is TextBox)
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.Key == Key.N)
        {
            New_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.Key == Key.O)
        {
            Open_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            e.Key == Key.S)
        {
            await SaveAsync(forceSaveAs: true);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.Key == Key.S)
        {
            await SaveAsync(forceSaveAs: false);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            ToolResult result = _viewModel.EnterGripEditModeForSelection();

            RefreshStatus();
            CadCanvas.ClearSnapMarker();
            CadCanvas.InvalidateVisual();
            CadCanvas.Focus();

            e.Handled = result.Changed;
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

    private void SubmitCommandInputText()
    {
        string input = CommandInputTextBox.Text ?? string.Empty;

        _viewModel.SubmitCommandInput(input);

        ClearCommandInputText();

        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
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
        _viewModel.SetRenderedEntityCount(CadCanvas.RenderedEntityCount);
        _viewModel.SetMousePosition(e.MousePosition);
        _viewModel.SetLastResult(e.Result);
        _viewModel.SetCurrentSnapCandidate(e.SnapCandidate);
        _viewModel.NotifyDocumentStateChanged();

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        _viewModel.SetRenderedEntityCount(CadCanvas.RenderedEntityCount);

        Title = _viewModel.WindowTitle;

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

    private void GridVisible_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetGridVisible(GridVisibleCheckBox.IsChecked == true);

        RefreshStatus();

        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private void GridSettingsTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        ApplyGridSettingsFromUi();

        e.Handled = true;
    }

    private void GridSettingsTextBox_LostFocus(
        object? sender,
        RoutedEventArgs e)
    {
        ApplyGridSettingsFromUi();
    }

    private void ApplyGridSettingsFromUi()
    {
        if (!TryParseGridValue(GridMinorStepTextBox.Text, out double minorStep) ||
            !TryParseGridValue(GridMajorStepTextBox.Text, out double majorStep) ||
            !TryParseGridValue(GridMinScreenSpacingTextBox.Text, out double minimumScreenSpacing) ||
            !TryParseGridValue(GridMaxScreenSpacingTextBox.Text, out double maximumScreenSpacing))
        {
            _viewModel.SetMessage("Invalid grid settings.");
            RefreshGridControls();
            RefreshStatus();
            return;
        }

        _viewModel.TrySetGridSettings(
            minorStep,
            majorStep,
            minimumScreenSpacing,
            maximumScreenSpacing,
            out _);

        RefreshGridControls();
        RefreshStatus();

        CadCanvas.InvalidateVisual();
        CadCanvas.Focus();
    }

    private static bool TryParseGridValue(
        string? text,
        out double value)
    {
        return double.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static string FormatGridValue(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
            CircleButton,
            activeToolName.Equals("Circle", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MoveButton,
            activeToolName.Equals("Move", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            CopyButton,
            activeToolName.Equals("Copy", StringComparison.OrdinalIgnoreCase));

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