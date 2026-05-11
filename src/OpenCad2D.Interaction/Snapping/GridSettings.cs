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
        double maximumScreenSpacing = 220,
        GridKind kind = GridKind.Rectangular,
        double isometricAngleDegrees = 30)
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

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                "Unsupported grid kind.");
        }

        if (isometricAngleDegrees <= 0 || isometricAngleDegrees >= 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(isometricAngleDegrees),
                "Isometric angle must be greater than 0 and less than 90 degrees.");
        }

        MinorStep = step;
        MajorStep = majorStep;
        OriginX = originX;
        OriginY = originY;
        IsVisible = isVisible;
        MinimumScreenSpacing = minimumScreenSpacing;
        MaximumScreenSpacing = maximumScreenSpacing;
        Kind = kind;
        IsometricAngleDegrees = isometricAngleDegrees;
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
    /// Layout used by grid rendering and grid snapping.
    /// </summary>
    public GridKind Kind { get; }

    /// <summary>
    /// Angle, in degrees, used by the two diagonal families of the isometric grid.
    /// The default is 30 degrees.
    /// </summary>
    public double IsometricAngleDegrees { get; }

    /// <summary>
    /// Returns the horizontal distance between isometric vertical lines for the given
    /// diagonal family spacing. This makes vertical lines pass through the vertices
    /// created by the intersections of the two diagonal families.
    /// </summary>
    public double GetIsometricVerticalStep(double diagonalSpacing)
    {
        if (diagonalSpacing <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(diagonalSpacing),
                "Isometric diagonal spacing must be greater than zero.");
        }

        double angleRadians = IsometricAngleDegrees * Math.PI / 180.0;
        double tangent = Math.Tan(angleRadians);

        return diagonalSpacing / (2.0 * tangent);
    }

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
            MaximumScreenSpacing,
            Kind,
            IsometricAngleDegrees);
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
            MaximumScreenSpacing,
            Kind,
            IsometricAngleDegrees);
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
            maximumScreenSpacing,
            Kind,
            IsometricAngleDegrees);
    }



    public GridSettings WithKind(
        GridKind kind,
        double isometricAngleDegrees)
    {
        return new GridSettings(
            MinorStep,
            OriginX,
            OriginY,
            IsVisible,
            MajorStep,
            MinimumScreenSpacing,
            MaximumScreenSpacing,
            kind,
            isometricAngleDegrees);
    }

    public GridSettings WithOrigin(
        double originX,
        double originY)
    {
        return new GridSettings(
            MinorStep,
            originX,
            originY,
            IsVisible,
            MajorStep,
            MinimumScreenSpacing,
            MaximumScreenSpacing,
            Kind,
            IsometricAngleDegrees);
    }

    public bool ShouldRenderScreenSpacing(double screenSpacing)
    {
        return screenSpacing >= MinimumScreenSpacing &&
               screenSpacing <= MaximumScreenSpacing;
    }
}
