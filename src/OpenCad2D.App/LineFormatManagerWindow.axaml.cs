using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.LineFormats;

namespace OpenCad2D.App;

public partial class LineFormatManagerWindow : Window
{
    public LineFormatManagerWindow()
    {
        InitializeComponent();
    }

    public LineFormatManagerWindow(LineFormatManagerWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    public LineFormatManagerResult? Result { get; private set; }

    private LineFormatManagerWindowViewModel? ViewModel =>
        DataContext as LineFormatManagerWindowViewModel;

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

        if (!ViewModel.TryBuildResult(out LineFormatManagerResult result))
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
