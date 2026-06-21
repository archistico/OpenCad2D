using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Architecture.Windows;

/// <summary>
/// Generated linework for a schematic parametric window.
/// </summary>
public sealed class WindowGeometry
{
    public WindowGeometry(
        IReadOnlyList<LineSegment2D> segments,
        IReadOnlyList<Point2D>? wallMaskPolygon = null)
    {
        Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        WallMaskPolygon = wallMaskPolygon ?? Array.Empty<Point2D>();
    }

    public IReadOnlyList<LineSegment2D> Segments { get; }

    /// <summary>
    /// Optional non-destructive wipeout polygon for hiding wall linework under the window opening.
    /// </summary>
    public IReadOnlyList<Point2D> WallMaskPolygon { get; }

    public bool HasWallMask => WallMaskPolygon.Count >= 3;
}
