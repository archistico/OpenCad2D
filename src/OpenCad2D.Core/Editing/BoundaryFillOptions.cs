using OpenCad2D.Geometry;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Options used by the boundary fill search.
/// </summary>
public sealed class BoundaryFillOptions
{
    public const int DefaultCurveSampleCount = 64;

    public BoundaryFillOptions(
        GeometryTolerance? geometryTolerance = null,
        double? gapTolerance = null,
        bool includeCurveBoundaries = false,
        int curveSampleCount = DefaultCurveSampleCount)
    {
        if (curveSampleCount < 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(curveSampleCount),
                "Curve sample count must be at least 8.");
        }

        GeometryTolerance = geometryTolerance ?? GeometryTolerance.Default;
        GapTolerance = gapTolerance ?? GeometryTolerance.Distance;
        IncludeCurveBoundaries = includeCurveBoundaries;
        CurveSampleCount = curveSampleCount;
    }

    public GeometryTolerance GeometryTolerance { get; }

    public double GapTolerance { get; }

    public bool IncludeCurveBoundaries { get; }

    public int CurveSampleCount { get; }

    public static BoundaryFillOptions FromTolerance(GeometryTolerance? tolerance)
    {
        return new BoundaryFillOptions(tolerance);
    }
}
