using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Blocks;
using System;
using System.Globalization;

namespace OpenCad2D.App;

public partial class CreateBlockOptionsWindow : Window
{
    public CreateBlockOptionsWindow()
    {
        InitializeComponent();

        Opened += (_, _) => NameTextBox.Focus();
    }

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string blockName = (NameTextBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(blockName))
        {
            NameTextBox.Focus();
            return;
        }

        if (!TryParseFinite(BasePointXTextBox.Text, out double basePointX))
        {
            BasePointXTextBox.Focus();
            return;
        }

        if (!TryParseFinite(BasePointYTextBox.Text, out double basePointY))
        {
            BasePointYTextBox.Focus();
            return;
        }

        Close(new CreateBlockOptions(
            blockName,
            basePointX,
            basePointY));
    }


    private void PickBasePoint_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string blockName = (NameTextBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(blockName))
        {
            NameTextBox.Focus();
            return;
        }

        Close(new CreateBlockOptions(
            blockName,
            0,
            0,
            PickBasePointFromDrawing: true));
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }

    private static bool TryParseFinite(
        string? text,
        out double value)
    {
        return double.TryParse(
                   text,
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out value) &&
               double.IsFinite(value);
    }
}
