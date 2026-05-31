namespace OpenCad2D.App.ViewModels;

/// <summary>
/// Classifies a numeric field displayed by the dynamic command HUD.
/// The kind is intentionally UI-facing and does not perform geometry changes by itself.
/// </summary>
public enum CommandHudFieldKind
{
    Generic,
    Distance,
    Angle,
    Width,
    Height,
    Radius,
    Factor,
    Sides,
    Segments,
    X,
    Y
}

/// <summary>
/// Numeric field displayed by the dynamic command HUD.
/// Fields are still rendered as read-only in the current UI, but the metadata below
/// prepares the future typed-override step without changing tool behavior yet.
/// </summary>
public sealed class CommandHudFieldViewModel
{
    public CommandHudFieldViewModel(
        string key,
        string label,
        double? liveValue,
        string? unit = null,
        bool isEditable = true,
        CommandHudFieldKind? kind = null)
    {
        Key = key;
        Label = label;
        LiveValue = liveValue;
        Unit = unit;
        IsEditable = isEditable;
        Kind = kind ?? InferKind(key, label, unit);
    }

    public string Key { get; }

    public string Label { get; }

    public double? LiveValue { get; }

    public string? Unit { get; }

    /// <summary>
    /// Indicates whether the field may become editable once the HUD override model is enabled.
    /// The current XAML still renders the value as read-only.
    /// </summary>
    public bool IsEditable { get; }

    public CommandHudFieldKind Kind { get; }

    public bool CanAcceptTypedOverride => IsEditable && Kind != CommandHudFieldKind.Generic;

    public string NumericValueText => LiveValue is null
        ? string.Empty
        : Kind == CommandHudFieldKind.Angle
            ? $"{LiveValue.Value:0.##}"
            : $"{LiveValue.Value:0.###}";

    public string DisplayValue => string.IsNullOrWhiteSpace(NumericValueText)
        ? string.Empty
        : Unit == "°"
            ? $"{NumericValueText}°"
            : NumericValueText;

    public string InputPlaceholder => Kind switch
    {
        CommandHudFieldKind.Distance => "distance",
        CommandHudFieldKind.Angle => "angle",
        CommandHudFieldKind.Width => "width",
        CommandHudFieldKind.Height => "height",
        CommandHudFieldKind.Radius => "radius",
        CommandHudFieldKind.Factor => "factor",
        CommandHudFieldKind.Sides => "sides",
        CommandHudFieldKind.Segments => "segments",
        CommandHudFieldKind.X => "x",
        CommandHudFieldKind.Y => "y",
        _ => string.Empty
    };

    private static CommandHudFieldKind InferKind(
        string key,
        string label,
        string? unit)
    {
        if (unit == "°" || ContainsInvariant(key, "angle") || ContainsInvariant(label, "angle"))
        {
            return CommandHudFieldKind.Angle;
        }

        if (ContainsInvariant(key, "width") || ContainsInvariant(label, "width"))
        {
            return CommandHudFieldKind.Width;
        }

        if (ContainsInvariant(key, "height") || ContainsInvariant(label, "height"))
        {
            return CommandHudFieldKind.Height;
        }

        if (ContainsInvariant(key, "radius") || ContainsInvariant(label, "radius"))
        {
            return CommandHudFieldKind.Radius;
        }

        if (ContainsInvariant(key, "factor") || ContainsInvariant(label, "factor"))
        {
            return CommandHudFieldKind.Factor;
        }

        if (ContainsInvariant(key, "sides") || ContainsInvariant(label, "sides"))
        {
            return CommandHudFieldKind.Sides;
        }

        if (ContainsInvariant(key, "segments") || ContainsInvariant(label, "segments"))
        {
            return CommandHudFieldKind.Segments;
        }

        if (string.Equals(key, "x", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(label, "x", System.StringComparison.OrdinalIgnoreCase))
        {
            return CommandHudFieldKind.X;
        }

        if (string.Equals(key, "y", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(label, "y", System.StringComparison.OrdinalIgnoreCase))
        {
            return CommandHudFieldKind.Y;
        }

        if (ContainsInvariant(key, "distance") || ContainsInvariant(label, "distance"))
        {
            return CommandHudFieldKind.Distance;
        }

        return CommandHudFieldKind.Generic;
    }

    private static bool ContainsInvariant(
        string value,
        string text)
    {
        return value.Contains(text, System.StringComparison.OrdinalIgnoreCase);
    }
}
