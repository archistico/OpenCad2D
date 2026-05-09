using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.Controls;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Layers;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using System;

namespace OpenCad2D.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        InitializeLayerComboBox();
        RefreshLayerVisibleCheckBox();

        RefreshStatus();
    }

    private void InitializeLayerComboBox()
    {
        LayerComboBox.ItemsSource = _viewModel.LayerNames;
        LayerComboBox.SelectedItem = _viewModel.CurrentLayer.Name;

        RefreshLayerVisibleCheckBox();
    }


    private void RefreshLayerVisibleCheckBox()
    {
        LayerVisibleCheckBox.IsChecked = _viewModel.CurrentLayer.IsVisible;
    }

    private void LayerVisibleCheckBox_Click(
        object? sender,
        RoutedEventArgs e)
    {
        bool isVisible = LayerVisibleCheckBox.IsChecked == true;

        _viewModel.SetCurrentLayerVisibility(isVisible);

        RefreshLayerVisibleCheckBox();
        RefreshStatus();

        CadCanvas.ClearSnapMarker();
        CadCanvas.InvalidateVisual();
    }

    private void LayerComboBox_SelectionChanged(
    object? sender,
    SelectionChangedEventArgs e)
    {
        if (LayerComboBox.SelectedItem is not string selectedLayerName)
        {
            return;
        }

        _viewModel.SetCurrentLayerByName(selectedLayerName);

        RefreshLayerVisibleCheckBox();
        RefreshStatus();

        CadCanvas.ClearSnapMarker();
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
        _viewModel.NotifyDocumentStateChanged();

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        Title = $"OpenCad2D - {_viewModel.ActiveToolName}";

        RefreshActiveToolUi();
    }

    private void SnapEndpoint_Changed(
    object? sender,
    RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Endpoint,
            SnapEndpointCheckBox.IsChecked == true);
    }

    private void SnapMidpoint_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Midpoint,
            SnapMidpointCheckBox.IsChecked == true);
    }

    private void SnapCenter_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Center,
            SnapCenterCheckBox.IsChecked == true);
    }

    private void SnapQuadrant_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Quadrant,
            SnapQuadrantCheckBox.IsChecked == true);
    }

    private void SnapIntersection_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Intersection,
            SnapIntersectionCheckBox.IsChecked == true);
    }

    private void SnapGrid_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Grid,
            SnapGridCheckBox.IsChecked == true);
    }

    private void SetSnapFromCheckBox(
        SnapKind snapKind,
        bool isEnabled)
    {
        _viewModel.SetSnapEnabled(
            snapKind,
            isEnabled);

        RefreshStatus();
        CadCanvas.ClearSnapMarker();
    }

    private void SnapPerpendicular_Changed(
    object? sender,
    RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Perpendicular,
            SnapPerpendicularCheckBox.IsChecked == true);
    }

    private void SnapTangent_Changed(
    object? sender,
    RoutedEventArgs e)
    {
        SetSnapFromCheckBox(
            SnapKind.Tangent,
            SnapTangentCheckBox.IsChecked == true);
    }

    private void RefreshActiveToolUi()
    {
        string activeToolName = _viewModel.ActiveToolName;

        SetActiveToolButton(
            SelectButton,
            activeToolName.Equals("Select", StringComparison.OrdinalIgnoreCase)
            || activeToolName.Equals("Selection", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            LineButton,
            activeToolName.Equals("Line", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            RectangleButton,
            activeToolName.Equals("Rectangle", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            MoveButton,
            activeToolName.Equals("Move", StringComparison.OrdinalIgnoreCase));

        SetActiveToolButton(
            CopyButton,
            activeToolName.Equals("Copy", StringComparison.OrdinalIgnoreCase));

        ActiveCommandTextBlock.Text =
            $"Comando attivo: {activeToolName}";
    }

    private static void SetActiveToolButton(
    Button button,
    bool isActive)
    {
        const string activeClassName = "active-tool";

        if (isActive)
        {
            if (!button.Classes.Contains(activeClassName))
            {
                button.Classes.Add(activeClassName);
            }

            return;
        }

        button.Classes.Remove(activeClassName);
    }
}