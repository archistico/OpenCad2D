using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenCad2D.App.ViewModels.Blocks;
using System;
using System.Globalization;

namespace OpenCad2D.App;

public partial class CreateBlockOptionsWindow : Window
{
    private readonly int _selectedEntityCount;

    public CreateBlockOptionsWindow()
        : this(0)
    {
    }

    public CreateBlockOptionsWindow(int selectedEntityCount)
        : this(selectedEntityCount, null)
    {
    }

    public CreateBlockOptionsWindow(
        int selectedEntityCount,
        CreateBlockOptions? initialOptions)
    {
        InitializeComponent();

        _selectedEntityCount = Math.Max(0, selectedEntityCount);

        if (initialOptions is not null)
        {
            NameTextBox.Text = initialOptions.Name;
            BasePointXTextBox.Text = initialOptions.BasePointX.ToString(CultureInfo.InvariantCulture);
            BasePointYTextBox.Text = initialOptions.BasePointY.ToString(CultureInfo.InvariantCulture);
        }

        UpdateSelectionState();

        Opened += (_, _) => NameTextBox.Focus();
    }

    private void UpdateSelectionState()
    {
        SelectedEntityCountTextBlock.Text = $"Entities in block: {_selectedEntityCount}";

        if (_selectedEntityCount == 0)
        {
            SelectionHelpTextBlock.Text = "No entity selected.";
            OkButton.IsEnabled = false;
            return;
        }

        SelectionHelpTextBlock.Text = "Ready to create a block.";
        OkButton.IsEnabled = true;
    }

    private void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_selectedEntityCount == 0)
        {
            SelectEntitiesButton.Focus();
            return;
        }

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
        if (_selectedEntityCount == 0)
        {
            SelectEntitiesButton.Focus();
            return;
        }

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

    private void SelectEntities_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string blockName = (NameTextBox.Text ?? string.Empty).Trim();

        double basePointX = TryParseFinite(BasePointXTextBox.Text, out double parsedX)
            ? parsedX
            : 0;

        double basePointY = TryParseFinite(BasePointYTextBox.Text, out double parsedY)
            ? parsedY
            : 0;

        Close(new CreateBlockOptions(
            blockName,
            basePointX,
            basePointY,
            PickEntitiesFromDrawing: true));
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
