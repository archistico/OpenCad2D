using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides snapping services and snapping settings for CAD tools.
/// </summary>
public sealed class ToolSnapContext
{
    public ToolSnapContext(
        SnapService service,
        SnapKind enabledSnaps,
        double tolerance,
        GridSettings gridSettings)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(gridSettings);

        if (tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Snap tolerance cannot be negative.");
        }

        Service = service;
        EnabledSnaps = enabledSnaps;
        Tolerance = tolerance;
        GridSettings = gridSettings;
    }

    public SnapService Service { get; }

    public SnapKind EnabledSnaps { get; set; }

    public double Tolerance { get; set; }

    public GridSettings GridSettings { get; set; }
}