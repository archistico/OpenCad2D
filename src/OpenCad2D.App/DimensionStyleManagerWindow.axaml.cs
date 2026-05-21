using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.DimensionStyles;

namespace OpenCad2D.App;

public partial class DimensionStyleManagerWindow : Window
{
    public DimensionStyleManagerWindow()
    {
        InitializeComponent();
    }

    public DimensionStyleManagerWindow(DimensionStyleManagerWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    public DimensionStyleManagerResult? Result { get; private set; }

    private DimensionStyleManagerWindowViewModel? ViewModel =>
        DataContext as DimensionStyleManagerWindowViewModel;

    private void AddStyle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.AddStyle();
    }

    private void DeleteStyle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.DeleteSelectedStyle();
    }

    private void SetCurrent_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.SetSelectedAsCurrent();
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

        if (!ViewModel.TryBuildResult(out DimensionStyleManagerResult result))
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
