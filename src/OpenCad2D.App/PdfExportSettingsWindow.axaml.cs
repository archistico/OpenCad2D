using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Pdf;
using OpenCad2D.Export.Pdf;
using System;

namespace OpenCad2D.App;

public partial class PdfExportSettingsWindow : Window
{
    private readonly PdfExportSettingsWindowViewModel _viewModel;

    public PdfExportSettingsWindow()
        : this(PdfExportOptions.Default)
    {
    }

    public PdfExportSettingsWindow(PdfExportOptions options)
    {
        InitializeComponent();

        _viewModel = new PdfExportSettingsWindowViewModel(options);
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
            Close(_viewModel.CreateOptions());
        }
        catch (Exception exception)
        {
            await ShowValidationMessageAsync(exception.Message);
        }
    }

    private void SynchronizeViewModelFromControls()
    {
        if (PageSizeComboBox.SelectedItem is PdfPageSize pageSize)
        {
            _viewModel.SelectedPageSize = pageSize;
        }

        if (OrientationComboBox.SelectedItem is PdfPageOrientation orientation)
        {
            _viewModel.SelectedOrientation = orientation;
        }

        _viewModel.MarginMillimetersText = MarginTextBox.Text ?? string.Empty;
        _viewModel.IncludeHiddenLayers = IncludeHiddenLayersCheckBox.IsChecked == true;
        _viewModel.UsePrintFriendlyColors = PrintFriendlyColorsCheckBox.IsChecked == true;
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
            Title = "Invalid PDF settings",
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
