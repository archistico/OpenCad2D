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

    public double? Width { get; set; }

    public double? Height { get; set; }

    public double? Radius { get; set; }

    public double? Factor { get; set; }

    public double? Sides { get; set; }

    public double? Segments { get; set; }

    public double? Gap { get; set; }

    public double? X { get; set; }

    public double? Y { get; set; }

    public bool HasPolarOverride =>
        Distance is not null || AngleDegrees is not null;

    public bool HasSizeOverride =>
        Width is not null || Height is not null || Radius is not null || Factor is not null;

    public bool HasNumberOverride =>
        Sides is not null || Segments is not null;

    public bool HasToolParameterOverride =>
        Gap is not null;

    public bool HasCoordinateOverride =>
        X is not null || Y is not null;

    public bool HasAnyOverride =>
        HasPolarOverride || HasSizeOverride || HasNumberOverride || HasToolParameterOverride || HasCoordinateOverride;

    public double? GetOverride(CommandHudFieldKind kind)
    {
        return kind switch
        {
            CommandHudFieldKind.Distance => Distance,
            CommandHudFieldKind.Angle => AngleDegrees,
            CommandHudFieldKind.Width => Width,
            CommandHudFieldKind.Height => Height,
            CommandHudFieldKind.Radius => Radius,
            CommandHudFieldKind.Factor => Factor,
            CommandHudFieldKind.Sides => Sides,
            CommandHudFieldKind.Segments => Segments,
            CommandHudFieldKind.Gap => Gap,
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
            case CommandHudFieldKind.Width:
                Width = value;
                break;
            case CommandHudFieldKind.Height:
                Height = value;
                break;
            case CommandHudFieldKind.Radius:
                Radius = value;
                break;
            case CommandHudFieldKind.Factor:
                Factor = value;
                break;
            case CommandHudFieldKind.Sides:
                Sides = value;
                break;
            case CommandHudFieldKind.Segments:
                Segments = value;
                break;
            case CommandHudFieldKind.Gap:
                Gap = value;
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
        Width = null;
        Height = null;
        Radius = null;
        Factor = null;
        Sides = null;
        Segments = null;
        Gap = null;
        X = null;
        Y = null;
        ActiveField = CommandHudFieldKind.Distance;
    }
}
