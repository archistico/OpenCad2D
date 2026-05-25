using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.ImportDrawing;
using System;
using System.Globalization;

namespace OpenCad2D.App;

public partial class ImportDrawingOptionsWindow : Window
{
    public ImportDrawingOptionsWindow()
    {
        InitializeComponent();

        Opened += (_, _) => ScaleTextBox.Focus();
    }

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (!double.TryParse(
                ScaleTextBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double scale) ||
            !double.IsFinite(scale) ||
            scale <= 0)
        {
            ScaleTextBox.Focus();
            return;
        }

        if (!double.TryParse(
                RotationTextBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double rotationDegrees) ||
            !double.IsFinite(rotationDegrees))
        {
            RotationTextBox.Focus();
            return;
        }

        Close(new OpenCad2DImportPlacementOptions(
            scale,
            rotationDegrees));
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }
}
