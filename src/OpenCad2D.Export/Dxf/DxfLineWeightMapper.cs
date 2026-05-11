using OpenCad2D.Core.Styling;

namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Maps OpenCad2D line weights to DXF group code 370 values.
/// </summary>
public static class DxfLineWeightMapper
{
    /// <summary>
    /// Converts the OpenCad2D graphic line-weight value to the DXF lineweight integer.
    /// DXF stores lineweight as hundredths of a millimetre; OpenCad2D treats the
    /// value as a graphic thickness and exports it pragmatically to that field.
    /// </summary>
    public static int ToDxfLineWeight(LineWeight lineWeight)
    {
        if (lineWeight.IsByLayer)
        {
            return -1;
        }

        return Math.Clamp(
            (int)Math.Round(lineWeight.Millimeters * 100.0, MidpointRounding.AwayFromZero),
            0,
            211);
    }
}
