using OpenCad2D.Core.Styling;

namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Maps OpenCad2D RGB colors to DXF color representations.
/// </summary>
public static class DxfColorMapper
{
    private static readonly DxfAciColor[] AciPalette =
    [
        new(1, 255, 0, 0),
        new(2, 255, 255, 0),
        new(3, 0, 255, 0),
        new(4, 0, 255, 255),
        new(5, 0, 0, 255),
        new(6, 255, 0, 255),
        new(7, 255, 255, 255),
    ];

    /// <summary>
    /// Returns the closest basic AutoCAD Color Index value.
    /// </summary>
    public static int ToAci(CadColor color)
    {
        if (color.IsByLayer)
        {
            return 256;
        }

        DxfAciColor best = AciPalette[0];
        int bestDistance = int.MaxValue;

        foreach (DxfAciColor candidate in AciPalette)
        {
            int distance = SquaredDistance(color, candidate);

            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best.Index;
    }

    /// <summary>
    /// Returns the DXF true-color integer used by group code 420.
    /// </summary>
    public static int ToTrueColor(CadColor color)
    {
        if (color.IsByLayer)
        {
            return 0;
        }

        return (color.R << 16) +
               (color.G << 8) +
               color.B;
    }

    private static int SquaredDistance(
        CadColor color,
        DxfAciColor candidate)
    {
        int red = color.R - candidate.Red;
        int green = color.G - candidate.Green;
        int blue = color.B - candidate.Blue;

        return (red * red) +
               (green * green) +
               (blue * blue);
    }

    private readonly record struct DxfAciColor(
        int Index,
        int Red,
        int Green,
        int Blue);
}
