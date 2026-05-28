using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.ImageReferences;

namespace OpenCad2D.App;

public partial class ImageReferenceManagerWindow : Window
{
    public ImageReferenceManagerWindow()
    {
        InitializeComponent();
    }

    public ImageReferenceManagerWindow(ImageReferenceManagerWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    public ImageReferenceManagerResult? Result { get; private set; }

    private ImageReferenceManagerWindowViewModel? ViewModel =>
        DataContext as ImageReferenceManagerWindowViewModel;

    private void SelectInDrawing_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CloseWithAction(ImageReferenceManagerAction.SelectInDrawing);
    }

    private void Relink_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CloseWithAction(ImageReferenceManagerAction.Relink);
    }

    private void Replace_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CloseWithAction(ImageReferenceManagerAction.Replace);
    }

    private void OpenFolder_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CloseWithAction(ImageReferenceManagerAction.OpenFolder);
    }

    private void ApplyTransparency_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ImageReferenceItemViewModel? selectedReference = ViewModel?.SelectedReference;

        if (selectedReference is null || ViewModel is null || !ViewModel.TryGetTransparencyPercent(out double transparencyPercent))
        {
            return;
        }

        Result = new ImageReferenceManagerResult(
            ImageReferenceManagerAction.SetTransparency,
            selectedReference,
            transparencyPercent);

        Close(Result);
    }

    private void Close_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }

    private void CloseWithAction(ImageReferenceManagerAction action)
    {
        ImageReferenceItemViewModel? selectedReference = ViewModel?.SelectedReference;

        if (selectedReference is null)
        {
            return;
        }

        Result = new ImageReferenceManagerResult(
            action,
            selectedReference);

        Close(Result);
    }
}
