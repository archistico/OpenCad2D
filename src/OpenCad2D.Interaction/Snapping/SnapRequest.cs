using OpenCad2D.Core.Documents;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Contains all information required to evaluate object snaps.
/// </summary>
public sealed class SnapRequest
{
    public SnapRequest(
        CadDocument document,
        Point2D cursorPoint,
        double tolerance,
        SnapKind enabledSnaps,
        Point2D? basePoint = null)
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
    }

    public CadDocument Document { get; }

    /// <summary>
    /// Current cursor position in model coordinates.
    /// </summary>
    public Point2D CursorPoint { get; }

    /// <summary>
    /// Optional base point used by contextual snaps such as perpendicular and tangent.
    /// </summary>
    public Point2D? BasePoint { get; }

    /// <summary>
    /// Snap tolerance expressed in model units.
    /// In the UI this will usually be converted from pixels.
    /// </summary>
    public double Tolerance { get; }

    public SnapKind EnabledSnaps { get; }

    public bool IsEnabled(SnapKind kind)
    {
        return EnabledSnaps.HasFlag(kind);
    }
}