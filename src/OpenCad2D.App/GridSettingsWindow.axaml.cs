using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Grid;

namespace OpenCad2D.App;

public partial class GridSettingsWindow : Window
{
    public GridSettingsWindow()
    {
        InitializeComponent();
    }

    public GridSettingsWindow(GridSettingsWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    public GridSettingsResult? Result { get; private set; }

    private GridSettingsWindowViewModel? ViewModel =>
        DataContext as GridSettingsWindowViewModel;

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            Close(null);
            return;
        }

        if (!ViewModel.TryBuildResult(out GridSettingsResult result))
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
