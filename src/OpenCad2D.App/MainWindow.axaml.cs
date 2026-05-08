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
        CadCanvas.ClearSnapMarker();
    }

    private void Line_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Line);
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void Rectangle_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Rectangle);
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void Move_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Move);
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void Copy_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.SetTool(ToolId.Copy);
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void Delete_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.DeleteSelection();
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void Undo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.Undo();
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void Redo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _viewModel.Redo();
        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void CadCanvas_WorkspaceChanged(
    object? sender,
    CadCanvasWorkspaceChangedEventArgs e)
    {
        _viewModel.SetMousePosition(e.MousePosition);
        _viewModel.SetLastResult(e.Result);
        _viewModel.SetCurrentSnapCandidate(e.SnapCandidate);

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        StatusTextBlock.Text = _viewModel.StatusText;
        Title = $"OpenCad2D - {_viewModel.ActiveToolName}";
    }
}