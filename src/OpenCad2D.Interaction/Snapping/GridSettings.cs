namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Describes model-space grid snapping and grid rendering settings.
/// </summary>
public sealed class GridSettings
{
    public GridSettings(
        double step = 10,
        double originX = 0,
        double originY = 0,
        bool isVisible = true,
        double majorStep = 50,
        double minimumScreenSpacing = 8,
        double maximumScreenSpacing = 220)
    {
        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(step),
                "Grid step must be greater than zero.");
        }

        if (majorStep <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorStep),
                "Major grid step must be greater than zero.");
        }

        if (majorStep < step)
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorStep),
                "Major grid step must be greater than or equal to minor grid step.");
        }

        if (minimumScreenSpacing <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumScreenSpacing),
                "Minimum screen spacing must be greater than zero.");
        }

        if (maximumScreenSpacing <= minimumScreenSpacing)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumScreenSpacing),
                "Maximum screen spacing must be greater than minimum screen spacing.");
        }

        MinorStep = step;
        MajorStep = majorStep;
        OriginX = originX;
        OriginY = originY;
        IsVisible = isVisible;
        MinimumScreenSpacing = minimumScreenSpacing;
        MaximumScreenSpacing = maximumScreenSpacing;
    }

    /// <summary>
    /// Compatibility alias used by grid snapping.
    /// </summary>
    public double Step => MinorStep;

    /// <summary>
    /// Secondary grid spacing in model units.
    /// </summary>
    public double MinorStep { get; }

    /// <summary>
    /// Primary grid spacing in model units.
    /// </summary>
    public double MajorStep { get; }

    public double OriginX { get; }

    public double OriginY { get; }

    /// <summary>
    /// Controls grid rendering only. Grid snapping is controlled separately by SnapKind.Grid.
    /// </summary>
    public bool IsVisible { get; }

    /// <summary>
    /// Grid lines whose screen spacing is smaller than this value are not rendered.
    /// </summary>
    public double MinimumScreenSpacing { get; }

    /// <summary>
    /// Grid lines whose screen spacing is larger than this value are not rendered.
    /// </summary>
    public double MaximumScreenSpacing { get; }

    public GridSettings WithVisibility(bool isVisible)
    {
        return new GridSettings(
            MinorStep,
            OriginX,
            OriginY,
            isVisible,
            MajorStep,
            MinimumScreenSpacing,
            MaximumScreenSpacing);
    }

    public GridSettings WithSpacing(
        double minorStep,
        double majorStep)
    {
        return new GridSettings(
            minorStep,
            OriginX,
            OriginY,
            IsVisible,
            majorStep,
            MinimumScreenSpacing,
            MaximumScreenSpacing);
    }

    public GridSettings WithScreenSpacingRange(
        double minimumScreenSpacing,
        double maximumScreenSpacing)
    {
        return new GridSettings(
            MinorStep,
            OriginX,
            OriginY,
            IsVisible,
            MajorStep,
            minimumScreenSpacing,
            maximumScreenSpacing);
    }

    public bool ShouldRenderScreenSpacing(double screenSpacing)
    {
        return screenSpacing >= MinimumScreenSpacing &&
               screenSpacing <= MaximumScreenSpacing;
    }
}
