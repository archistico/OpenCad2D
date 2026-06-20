using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels;

/// <summary>
/// Centralizes the keyboard routing rules for the dynamic command HUD.
/// Keeping these rules outside MainWindow prevents every new tool phase from
/// duplicating fragile preferred-field heuristics.
/// </summary>
public static class CommandHudFieldRoutingPolicy
{
    private static readonly CommandHudFieldKind[] InitialNumericPriority =
    {
        CommandHudFieldKind.Distance,
        CommandHudFieldKind.Radius,
        CommandHudFieldKind.Width,
        CommandHudFieldKind.Height,
        CommandHudFieldKind.Factor,
        CommandHudFieldKind.Angle,
        CommandHudFieldKind.Sides,
        CommandHudFieldKind.Segments,
        CommandHudFieldKind.X,
        CommandHudFieldKind.Y,
        CommandHudFieldKind.Gap
    };

    public static CommandHudFieldKind? GetDefaultFieldKindForNumericText(
        IEnumerable<CommandHudFieldKind> availableKinds,
        CommandHudFieldKind? activeKind)
    {
        ArgumentNullException.ThrowIfNull(availableKinds);

        CommandHudFieldKind[] distinctKinds = availableKinds
            .Distinct()
            .ToArray();

        if (activeKind is not null &&
            distinctKinds.Contains(activeKind.Value))
        {
            return activeKind.Value;
        }

        foreach (CommandHudFieldKind preferredKind in InitialNumericPriority)
        {
            if (distinctKinds.Contains(preferredKind))
            {
                return preferredKind;
            }
        }

        return distinctKinds.FirstOrDefault(kind => kind != CommandHudFieldKind.Generic) is { } fallback &&
               fallback != CommandHudFieldKind.Generic
            ? fallback
            : null;
    }

    public static bool IsNumericHudText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (char character in text)
        {
            if (!char.IsDigit(character) &&
                character is not '.' and not ',' and not '-' and not '+')
            {
                return false;
            }
        }

        return true;
    }
}
