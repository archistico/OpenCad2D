using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Architecture.Stairs;

/// <summary>
/// Generated 2D linework for a stair entity.
/// </summary>
public sealed class StairGeometry
{
    public StairGeometry(IReadOnlyList<LineSegment2D> primarySegments)
        : this(primarySegments, Array.Empty<LineSegment2D>())
    {
    }

    public StairGeometry(
        IReadOnlyList<LineSegment2D> primarySegments,
        IReadOnlyList<LineSegment2D> annotationSegments)
    {
        ArgumentNullException.ThrowIfNull(primarySegments);
        ArgumentNullException.ThrowIfNull(annotationSegments);

        PrimarySegments = primarySegments.ToArray();
        AnnotationSegments = annotationSegments.ToArray();
        Segments = PrimarySegments.Concat(AnnotationSegments).ToArray();
    }

    /// <summary>
    /// Structural stair linework used for hit-test distance and snap candidates.
    /// </summary>
    public IReadOnlyList<LineSegment2D> PrimarySegments { get; }

    /// <summary>
    /// Conventional plan symbols such as direction arrows and break/section markers.
    /// </summary>
    public IReadOnlyList<LineSegment2D> AnnotationSegments { get; }

    /// <summary>
    /// All generated linework intended for rendering and export.
    /// </summary>
    public IReadOnlyList<LineSegment2D> Segments { get; }
}
