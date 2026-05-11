using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenCad2D.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
