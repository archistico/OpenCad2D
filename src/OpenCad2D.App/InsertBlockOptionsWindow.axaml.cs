using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Blocks;
using OpenCad2D.Core.Blocks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenCad2D.App;

public partial class InsertBlockOptionsWindow : Window
{
    private readonly IReadOnlyList<BlockDefinition> _definitions;

    public InsertBlockOptionsWindow()
        : this(Array.Empty<BlockDefinition>())
    {
    }

    public InsertBlockOptionsWindow(
        IReadOnlyList<BlockDefinition> definitions)
    {
        InitializeComponent();

        _definitions = definitions;
        BlockComboBox.ItemsSource = _definitions;

        if (_definitions.Count > 0)
        {
            BlockComboBox.SelectedIndex = 0;
        }

        Opened += (_, _) => BlockComboBox.Focus();
    }

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (BlockComboBox.SelectedItem is not BlockDefinition definition)
        {
            BlockComboBox.Focus();
            return;
        }

        if (!TryParsePositiveFinite(ScaleTextBox.Text, out double scale))
        {
            ScaleTextBox.Focus();
            return;
        }

        if (!TryParseFinite(RotationTextBox.Text, out double rotationDegrees))
        {
            RotationTextBox.Focus();
            return;
        }

        Close(new InsertBlockOptions(
            definition.Id,
            definition.Name,
            scale,
            rotationDegrees));
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }

    private static bool TryParsePositiveFinite(
        string? text,
        out double value)
    {
        return TryParseFinite(text, out value) && value > 0;
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
