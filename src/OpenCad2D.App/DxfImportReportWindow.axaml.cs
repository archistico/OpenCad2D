using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.DxfImport;
using OpenCad2D.Export.Dxf.Import;
using System;

namespace OpenCad2D.App;

public partial class DxfImportReportWindow : Window
{
    public DxfImportReportWindow()
    {
        InitializeComponent();
    }

    public DxfImportReportWindow(DxfImportResult result)
        : this()
    {
        ArgumentNullException.ThrowIfNull(result);

        DataContext = new DxfImportReportWindowViewModel(result);
    }

    private void Close_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
