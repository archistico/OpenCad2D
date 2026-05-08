namespace OpenCad2D.Core.Styling;

/// <summary>
/// Represents an entity color.
/// </summary>
public readonly record struct CadColor
{
    private CadColor(byte r, byte g, byte b, bool isByLayer)
    {
        R = r;
        G = g;
        B = b;
        IsByLayer = isByLayer;
    }

    public byte R { get; }

    public byte G { get; }

    public byte B { get; }

    public bool IsByLayer { get; }

    public static CadColor ByLayer => new(0, 0, 0, true);

    public static CadColor FromRgb(byte r, byte g, byte b)
    {
        return new CadColor(r, g, b, false);
    }
}