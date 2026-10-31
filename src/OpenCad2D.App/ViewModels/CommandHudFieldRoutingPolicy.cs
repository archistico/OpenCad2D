using System;
using System.Collections.Generic;
using System.Linq;
using OpenCad2D.Tools.Input;

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
        CommandHudFieldKind.Number,
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

    public static bool IsPointExpression(string text, CommandInputKind expectedInput)
    {
        if (text.StartsWith('@') || text.Contains('<'))
        {
            return true;
        }

        return text.Contains(',') && expectedInput is
            CommandInputKind.Point or CommandInputKind.PointOrOption or
            CommandInputKind.PointOrDistance or CommandInputKind.PointOrDistanceOrOption or
            CommandInputKind.PointOrAngle or CommandInputKind.PointOrAngleOrOption or
            CommandInputKind.PointOrNumber or CommandInputKind.PointOrNumberOrOption;
    }

    public static CommandHudFieldKind? GetAdjacentFieldKind(
        IEnumerable<CommandHudFieldKind> availableKinds,
        CommandHudFieldKind? activeKind,
        bool backwards = false)
    {
        ArgumentNullException.ThrowIfNull(availableKinds);

        CommandHudFieldKind[] kinds = availableKinds
            .Where(kind => kind != CommandHudFieldKind.Generic)
            .Distinct()
            .ToArray();

        if (kinds.Length == 0)
        {
            return null;
        }

        int currentIndex = activeKind is null ? -1 : Array.IndexOf(kinds, activeKind.Value);
        if (currentIndex < 0)
        {
            return backwards ? kinds[^1] : kinds[0];
        }

        return kinds[(currentIndex + (backwards ? kinds.Length - 1 : 1)) % kinds.Length];
    }
}
