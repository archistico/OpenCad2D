using System;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.App.ViewModels.PolarTracking;

/// <summary>
/// Represents one selectable polar tracking option in the main toolbar.
/// </summary>
public sealed class PolarTrackingOptionViewModel
{
    public PolarTrackingOptionViewModel(
        string displayName,
        AngleConstraintSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(settings);

        DisplayName = displayName;
        Settings = settings;
    }

    public string DisplayName { get; }

    public AngleConstraintSettings Settings { get; }

    public bool IsOff => !Settings.IsEnabled;

    public double? StepDegrees => Settings.IsEnabled
        ? Settings.StepDegrees
        : null;

    public override string ToString() => DisplayName;
}
