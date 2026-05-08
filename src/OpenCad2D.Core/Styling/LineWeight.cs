namespace OpenCad2D.Core.Styling;

/// <summary>
/// Represents a line weight in millimeters.
/// </summary>
public readonly record struct LineWeight
{
    private LineWeight(double millimeters, bool isByLayer)
    {
        if (!isByLayer && millimeters < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(millimeters),
                "Line weight cannot be negative.");
        }

        Millimeters = millimeters;
        IsByLayer = isByLayer;
    }

    public double Millimeters { get; }

    public bool IsByLayer { get; }

    public static LineWeight ByLayer => new(0, true);

    public static LineWeight FromMillimeters(double millimeters)
    {
        return new LineWeight(millimeters, false);
    }
}