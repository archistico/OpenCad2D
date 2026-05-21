using System;
using System.Collections.Generic;
using System.Linq;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.App.Settings;

public sealed class ApplicationSnapSettings
{
    public bool IsEnabled { get; set; } = true;

    public List<string> EnabledModes { get; set; } = new();

    public double Tolerance { get; set; } = 8;

    public static ApplicationSnapSettings FromSnapSettings(
        SnapKind enabledSnaps,
        double tolerance)
    {
        return new ApplicationSnapSettings
        {
            IsEnabled = enabledSnaps != SnapKind.None,
            EnabledModes = GetEnabledSnapModeNames(enabledSnaps).ToList(),
            Tolerance = tolerance
        };
    }

    public ApplicationSnapSettings Normalize()
    {
        EnabledModes = EnabledModes
            .Where(mode => Enum.TryParse(mode, ignoreCase: true, out SnapKind parsed) &&
                           parsed != SnapKind.None &&
                           parsed != SnapKind.All)
            .Select(mode => Enum.Parse<SnapKind>(mode, ignoreCase: true).ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (Tolerance <= 0)
        {
            Tolerance = 8;
        }

        return this;
    }

    public SnapKind ToSnapKind()
    {
        if (!IsEnabled)
        {
            return SnapKind.None;
        }

        SnapKind result = SnapKind.None;

        foreach (string mode in EnabledModes)
        {
            if (Enum.TryParse(mode, ignoreCase: true, out SnapKind parsed) &&
                parsed != SnapKind.None &&
                parsed != SnapKind.All)
            {
                result |= parsed;
            }
        }

        return result;
    }

    private static IEnumerable<string> GetEnabledSnapModeNames(SnapKind snapKind)
    {
        foreach (SnapKind value in Enum.GetValues<SnapKind>())
        {
            if (value is SnapKind.None or SnapKind.All)
            {
                continue;
            }

            if (snapKind.HasFlag(value))
            {
                yield return value.ToString();
            }
        }
    }
}
