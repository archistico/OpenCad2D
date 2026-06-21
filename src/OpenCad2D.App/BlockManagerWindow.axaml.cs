using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Blocks;

namespace OpenCad2D.App;

public partial class BlockManagerWindow : Window
{
    public BlockManagerWindow()
    {
        InitializeComponent();
    }

    public BlockManagerWindow(BlockManagerWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private BlockManagerWindowViewModel? ViewModel =>
        DataContext as BlockManagerWindowViewModel;

    private void Insert_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CloseWithAction(BlockManagerAction.InsertSelected);
    }

    private void Duplicate_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.DuplicateSelectedBlock();
    }

    private void Delete_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.DeleteSelectedBlock();
    }

    private void PurgeUnused_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.PurgeUnusedBlocks();
    }

    private void ResetNames_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?.ResetBlockNames();
    }

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CloseWithAction(BlockManagerAction.Close);
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }

    private void CloseWithAction(BlockManagerAction action)
    {
        if (ViewModel is null)
        {
            Close(null);
            return;
        }

        if (!ViewModel.TryBuildResult(action, out BlockManagerResult result))
        {
            return;
        }

        Close(result);
    }
}
