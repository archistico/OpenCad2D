using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Architecture.Stairs;

/// <summary>
/// Generated 2D linework for a stair entity.
/// </summary>
public sealed class StairGeometry
{
    public StairGeometry(IReadOnlyList<LineSegment2D> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        Segments = segments.ToArray();
    }

    public IReadOnlyList<LineSegment2D> Segments { get; }
}
