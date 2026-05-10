using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Layers;

namespace OpenCad2D.App;

public partial class LayerManagerWindow : Window
{
    public LayerManagerWindow()
    {
        InitializeComponent();
    }

    public LayerManagerWindow(LayerManagerWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    public LayerManagerResult? Result { get; private set; }

    private LayerManagerWindowViewModel? ViewModel =>
        DataContext as LayerManagerWindowViewModel;

    private void NewLayer_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.AddLayer();
    }

    private void DeleteLayer_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.DeleteSelectedLayer();
    }

    private void CurrentLayerRadio_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton ||
            radioButton.DataContext is not EditableLayerViewModel layer ||
            ViewModel is null)
        {
            return;
        }

        ViewModel.SetCurrentLayer(layer);
    }

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            Close(null);
            return;
        }

        if (!ViewModel.TryBuildResult(out LayerManagerResult result))
        {
            return;
        }

        Result = result;
        Close(result);
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }
}
