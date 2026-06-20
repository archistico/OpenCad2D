using OpenCad2D.Geometry;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Options used by Boundary Fill detection.
/// </summary>
public sealed class BoundaryFillOptions
{
    public BoundaryFillOptions(
        GeometryTolerance? geometryTolerance = null,
        double gapTolerance = 0.0,
        bool includeCurveBoundaries = false,
        int curveSampleCount = 64)
    {
        if (gapTolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gapTolerance),
                "Gap tolerance cannot be negative.");
        }

        if (curveSampleCount < 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(curveSampleCount),
                "Curve sample count must be at least 4.");
        }

        GeometryTolerance = geometryTolerance ?? GeometryTolerance.Default;
        GapTolerance = gapTolerance;
        IncludeCurveBoundaries = includeCurveBoundaries;
        CurveSampleCount = curveSampleCount;
    }

    public static BoundaryFillOptions Default { get; } = new();

    public GeometryTolerance GeometryTolerance { get; }

    public double GapTolerance { get; }

    /// <summary>
    /// Reserved for the next Boundary Fill v2 step that will add sampled arc/circle boundaries.
    /// </summary>
    public bool IncludeCurveBoundaries { get; }

    /// <summary>
    /// Reserved for sampled curve boundary extraction.
    /// </summary>
    public int CurveSampleCount { get; }

    public static BoundaryFillOptions FromTolerance(GeometryTolerance? tolerance)
    {
        return tolerance is null
            ? Default
            : new BoundaryFillOptions(tolerance);
    }
}
