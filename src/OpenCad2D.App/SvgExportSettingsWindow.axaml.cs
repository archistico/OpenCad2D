using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Svg;
using OpenCad2D.Export.Svg;
using System;

namespace OpenCad2D.App;

public partial class SvgExportSettingsWindow : Window
{
    private readonly SvgExportSettingsWindowViewModel _viewModel;
    private readonly string _title;

    public SvgExportSettingsWindow()
        : this("OpenCad2D", SvgExportOptions.Default)
    {
    }

    public SvgExportSettingsWindow(string title)
        : this(title, SvgExportOptions.Default)
    {
    }

    public SvgExportSettingsWindow(
        string title,
        SvgExportOptions options)
    {
        InitializeComponent();

        _title = title;
        _viewModel = new SvgExportSettingsWindowViewModel(options);
        DataContext = _viewModel;
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }

    private async void Export_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            SynchronizeViewModelFromControls();
            Close(_viewModel.CreateOptions(_title));
        }
        catch (Exception exception)
        {
            await ShowValidationMessageAsync(exception.Message);
        }
    }

    private void SynchronizeViewModelFromControls()
    {
        if (BackgroundModeComboBox.SelectedItem is SvgBackgroundMode backgroundMode)
        {
            _viewModel.SelectedBackgroundMode = backgroundMode;
        }

        _viewModel.MarginText = MarginTextBox.Text ?? string.Empty;
        _viewModel.GroupByLayer = GroupByLayerCheckBox.IsChecked == true;
        _viewModel.IncludeHiddenLayers = IncludeHiddenLayersCheckBox.IsChecked == true;
        _viewModel.IncludeMetadata = IncludeMetadataCheckBox.IsChecked == true;
    }

    private async System.Threading.Tasks.Task ShowValidationMessageAsync(string message)
    {
        var closeButton = new Button
        {
            Content = "OK",
            MinWidth = 86
        };

        var dialog = new Window
        {
            Title = "Invalid SVG settings",
            Width = 420,
            Height = 170,
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
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children = { closeButton }
                    }
                }
            }
        };

        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }
}
