using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Tools.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenCad2D.App;

public partial class TextInputWindow : Window
{
    private readonly IReadOnlyList<TextFormat> _formats;

    public TextInputWindow()
        : this(new TextInputRequest(
            OpenCad2D.Geometry.Primitives.Point2D.Origin,
            TextFormatId.Standard,
            0,
            TextFormatCollection.Default.All))
    {
    }

    public TextInputWindow(TextInputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        InitializeComponent();

        _formats = request.TextFormats.Count > 0
            ? request.TextFormats
            : TextFormatCollection.Default.All;

        TextFormatComboBox.ItemsSource = _formats
            .Select(format => format.Name)
            .ToList();

        int selectedIndex = Math.Max(
            0,
            _formats.ToList().FindIndex(format => format.Id == request.DefaultTextFormatId));

        TextFormatComboBox.SelectedIndex = selectedIndex;
        RotationTextBox.Text = request.DefaultRotationDegrees.ToString("0.###", CultureInfo.InvariantCulture);

        Opened += (_, _) => TextValueTextBox.Focus();
    }

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string text = TextValueTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            TextValueTextBox.Focus();
            return;
        }

        if (!double.TryParse(
                RotationTextBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double rotationDegrees))
        {
            rotationDegrees = 0;
        }

        int selectedIndex = TextFormatComboBox.SelectedIndex;
        TextFormat selectedFormat = selectedIndex >= 0 && selectedIndex < _formats.Count
            ? _formats[selectedIndex]
            : _formats[0];

        Close(new TextInputResult(
            text,
            selectedFormat.Id,
            rotationDegrees));
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }
}
