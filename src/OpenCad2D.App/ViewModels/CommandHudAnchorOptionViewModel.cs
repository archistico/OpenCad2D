using System;
using System.Globalization;
using OpenCad2D.Core.Anchors;

namespace OpenCad2D.App.ViewModels;

/// <summary>
/// Single visual cell of the command HUD 3x3 anchor selector.
/// The selector is keyboard-oriented: mouse hit testing stays disabled by the HUD overlay.
/// </summary>
public sealed class CommandHudAnchorOptionViewModel
{
    public CommandHudAnchorOptionViewModel(
        AnchorPointDescriptor descriptor,
        AnchorPoint selectedAnchor)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Anchor = descriptor.Anchor;
        Key = descriptor.Key;
        DisplayName = descriptor.DisplayName;
        Row = descriptor.Row;
        Column = descriptor.Column;
        NumericShortcut = descriptor.NumericShortcut;
        IsSelected = descriptor.Anchor == selectedAnchor;
    }

    public AnchorPointDescriptor Descriptor { get; }

    public AnchorPoint Anchor { get; }

    public string Key { get; }

    public string DisplayName { get; }

    public int Row { get; }

    public int Column { get; }

    public int NumericShortcut { get; }

    public bool IsSelected { get; }

    public string NumericShortcutText => NumericShortcut.ToString(CultureInfo.InvariantCulture);

    public string SelectionMarker => IsSelected ? "●" : "○";

    public string ShortDisplayName => Anchor switch
    {
        AnchorPoint.TopLeft => "TL",
        AnchorPoint.TopCenter => "TC",
        AnchorPoint.TopRight => "TR",
        AnchorPoint.MiddleLeft => "ML",
        AnchorPoint.Center => "C",
        AnchorPoint.MiddleRight => "MR",
        AnchorPoint.BottomLeft => "BL",
        AnchorPoint.BottomCenter => "BC",
        AnchorPoint.BottomRight => "BR",
        _ => Key
    };

    public string DisplayText => $"{NumericShortcutText} {ShortDisplayName}";

    public string TooltipText => $"{DisplayName} ({NumericShortcutText})";
}
