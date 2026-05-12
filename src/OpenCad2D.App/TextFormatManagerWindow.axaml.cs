using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.TextFormats;

namespace OpenCad2D.App;

public partial class TextFormatManagerWindow : Window
{
    public TextFormatManagerWindow()
    {
        InitializeComponent();
    }

    public TextFormatManagerWindow(TextFormatManagerWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    public TextFormatManagerResult? Result { get; private set; }

    private TextFormatManagerWindowViewModel? ViewModel =>
        DataContext as TextFormatManagerWindowViewModel;

    private void AddFormat_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.AddFormat();
    }

    private void DeleteFormat_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.DeleteSelectedFormat();
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

        if (!ViewModel.TryBuildResult(out TextFormatManagerResult result))
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
