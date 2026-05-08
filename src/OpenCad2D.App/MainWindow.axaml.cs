using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.Controls;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        RefreshStatus();
    }

    private void Select_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Selection);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void Line_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Line);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void Rectangle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Rectangle);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void Move_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Move);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void Copy_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Copy);
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void Delete_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.DeleteSelection();
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void Undo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.Undo();
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void Redo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.Redo();
        RefreshStatus();
        CadCanvas.InvalidateVisual();
    }

    private void CadCanvas_WorkspaceChanged(
        object? sender,
        CadCanvasWorkspaceChangedEventArgs e)
    {
        _viewModel.SetMousePosition(e.MousePosition);
        _viewModel.SetLastResult(e.Result);

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        StatusTextBlock.Text = _viewModel.StatusText;
        Title = $"OpenCad2D - {_viewModel.ActiveToolName}";
    }
}