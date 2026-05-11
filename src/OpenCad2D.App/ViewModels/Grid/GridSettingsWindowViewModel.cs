using OpenCad2D.Interaction.Snapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Grid;

public sealed class GridSettingsWindowViewModel : INotifyPropertyChanged
{
    private string _validationMessage = string.Empty;

    public GridSettingsWindowViewModel(GridSettings gridSettings)
    {
        ArgumentNullException.ThrowIfNull(gridSettings);

        GridKinds = new[]
        {
            GridKind.Rectangular.ToString(),
            GridKind.Isometric.ToString()
        };

        SelectedGridKind = gridSettings.Kind.ToString();
        IsVisible = gridSettings.IsVisible;
        MinorStep = ToText(gridSettings.MinorStep);
        MajorStep = ToText(gridSettings.MajorStep);
        OriginX = ToText(gridSettings.OriginX);
        OriginY = ToText(gridSettings.OriginY);
        MinimumScreenSpacing = ToText(gridSettings.MinimumScreenSpacing);
        MaximumScreenSpacing = ToText(gridSettings.MaximumScreenSpacing);
        IsometricAngleDegrees = ToText(gridSettings.IsometricAngleDegrees);
    }

    public IReadOnlyList<string> GridKinds { get; }

    public string SelectedGridKind { get; set; }

    public bool IsVisible { get; set; }

    public string MinorStep { get; set; }

    public string MajorStep { get; set; }

    public string OriginX { get; set; }

    public string OriginY { get; set; }

    public string MinimumScreenSpacing { get; set; }

    public string MaximumScreenSpacing { get; set; }

    public string IsometricAngleDegrees { get; set; }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage == value)
            {
                return;
            }

            _validationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidationMessage));
        }
    }

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    public bool TryBuildResult(out GridSettingsResult result)
    {
        result = new GridSettingsResult(new GridSettings());

        if (!Enum.TryParse(
                SelectedGridKind,
                ignoreCase: true,
                out GridKind gridKind))
        {
            ValidationMessage = "Select a valid grid type.";
            return false;
        }

        if (!TryParseDouble(MinorStep, "Minor step", out double minorStep) ||
            !TryParseDouble(MajorStep, "Major step", out double majorStep) ||
            !TryParseDouble(OriginX, "Origin X", out double originX) ||
            !TryParseDouble(OriginY, "Origin Y", out double originY) ||
            !TryParseDouble(MinimumScreenSpacing, "Minimum screen spacing", out double minimumScreenSpacing) ||
            !TryParseDouble(MaximumScreenSpacing, "Maximum screen spacing", out double maximumScreenSpacing) ||
            !TryParseDouble(IsometricAngleDegrees, "Isometric angle", out double isometricAngleDegrees))
        {
            return false;
        }

        try
        {
            var gridSettings = new GridSettings(
                step: minorStep,
                originX: originX,
                originY: originY,
                isVisible: IsVisible,
                majorStep: majorStep,
                minimumScreenSpacing: minimumScreenSpacing,
                maximumScreenSpacing: maximumScreenSpacing,
                kind: gridKind,
                isometricAngleDegrees: isometricAngleDegrees);

            result = new GridSettingsResult(gridSettings);
            ValidationMessage = string.Empty;
            return true;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            ValidationMessage = exception.Message;
            return false;
        }
    }

    private bool TryParseDouble(
        string? text,
        string fieldName,
        out double value)
    {
        string normalizedText = (text ?? string.Empty).Trim().Replace(',', '.');

        if (!double.TryParse(
                normalizedText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
        {
            ValidationMessage = $"{fieldName} must be a valid number.";
            return false;
        }

        return true;
    }

    private static string ToText(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
