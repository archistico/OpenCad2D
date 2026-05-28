using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Library;

namespace OpenCad2D.App;

public partial class LibraryWindow : Window
{
    public LibraryWindow()
    {
        InitializeComponent();
    }

    public LibraryWindow(LibraryWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private LibraryWindowViewModel? ViewModel =>
        DataContext as LibraryWindowViewModel;

    private void Insert_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            Close(null);
            return;
        }

        if (!ViewModel.TryBuildResult(out LibraryWindowResult result))
        {
            return;
        }

        Close(result);
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }
}
