namespace OpenCad2D.App.ViewModels;

/// <summary>
/// Persistent keyboard-driven input state for the dynamic command HUD.
/// It is intentionally generic and can serve any command that exposes polar
/// fields (distance/angle) or absolute coordinate fields (X/Y).
/// </summary>
public sealed class CommandHudInputState
{
    public CommandHudFieldKind ActiveField { get; set; } = CommandHudFieldKind.Distance;

    public double? Distance { get; set; }

    public double? AngleDegrees { get; set; }

    public double? X { get; set; }

    public double? Y { get; set; }

    public bool HasPolarOverride =>
        Distance is not null || AngleDegrees is not null;

    public bool HasCoordinateOverride =>
        X is not null || Y is not null;

    public bool HasAnyOverride =>
        HasPolarOverride || HasCoordinateOverride;

    public double? GetOverride(CommandHudFieldKind kind)
    {
        return kind switch
        {
            CommandHudFieldKind.Distance => Distance,
            CommandHudFieldKind.Angle => AngleDegrees,
            CommandHudFieldKind.X => X,
            CommandHudFieldKind.Y => Y,
            _ => null
        };
    }

    public void SetOverride(
        CommandHudFieldKind kind,
        double value)
    {
        ActiveField = kind;

        switch (kind)
        {
            case CommandHudFieldKind.Distance:
                Distance = value;
                break;
            case CommandHudFieldKind.Angle:
                AngleDegrees = value;
                break;
            case CommandHudFieldKind.X:
                X = value;
                break;
            case CommandHudFieldKind.Y:
                Y = value;
                break;
        }
    }

    public void Clear()
    {
        Distance = null;
        AngleDegrees = null;
        X = null;
        Y = null;
        ActiveField = CommandHudFieldKind.Distance;
    }
}
