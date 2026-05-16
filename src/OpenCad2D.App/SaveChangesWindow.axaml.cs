using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenCad2D.App;

public partial class SaveChangesWindow : Window
{
    public SaveChangesWindow()
    {
        InitializeComponent();
    }

    public SaveChangesWindow(string fileName)
        : this()
    {
        FileNameTextBlock.Text = string.IsNullOrWhiteSpace(fileName)
            ? "Untitled"
            : fileName;
    }

    private void Save_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(SaveChangesChoice.Save);
    }

    private void DontSave_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(SaveChangesChoice.DontSave);
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(SaveChangesChoice.Cancel);
    }
}
