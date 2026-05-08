namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Describes model-space grid snapping settings.
/// </summary>
public sealed class GridSettings
{
    public GridSettings(
        double step = 10,
        double originX = 0,
        double originY = 0)
    {
        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(step),
                "Grid step must be greater than zero.");
        }

        Step = step;
        OriginX = originX;
        OriginY = originY;
    }

    public double Step { get; }

    public double OriginX { get; }

    public double OriginY { get; }
}