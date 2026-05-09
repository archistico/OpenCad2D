using OpenCad2D.Core.Documents;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Describes a snapping request at a given cursor point.
/// </summary>
public sealed class SnapRequest
{
    public SnapRequest(
        CadDocument document,
        Point2D cursorPoint,
        double tolerance,
        SnapKind enabledSnaps,
        Point2D? basePoint = null,
        GridSettings? gridSettings = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Snap tolerance cannot be negative.");
        }

        Document = document;
        CursorPoint = cursorPoint;
        Tolerance = tolerance;
        EnabledSnaps = enabledSnaps;
        BasePoint = basePoint;
        GridSettings = gridSettings ?? new GridSettings();
    }

    public CadDocument Document { get; }

    public Point2D CursorPoint { get; }

    public Point2D? BasePoint { get; }

    public double Tolerance { get; }

    public SnapKind EnabledSnaps { get; }

    public GridSettings GridSettings { get; }

    public bool IsEnabled(SnapKind kind)
    {
        return EnabledSnaps.HasFlag(kind);
    }

    public BoundingBox2D SearchArea => new(
        CursorPoint.X - Tolerance,
        CursorPoint.Y - Tolerance,
        CursorPoint.X + Tolerance,
        CursorPoint.Y + Tolerance);
}