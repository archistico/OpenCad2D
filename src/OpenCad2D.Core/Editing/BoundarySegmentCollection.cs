namespace OpenCad2D.Core.Editing;

/// <summary>
/// Boundary segments and collection diagnostics gathered before graph construction.
/// </summary>
public sealed class BoundarySegmentCollection
{
    public BoundarySegmentCollection(
        IReadOnlyList<BoundarySegment> segments,
        int ignoredEntityCount,
        int sampledCurveSegmentCount)
    {
        Segments = segments;
        IgnoredEntityCount = ignoredEntityCount;
        SampledCurveSegmentCount = sampledCurveSegmentCount;
    }

    public IReadOnlyList<BoundarySegment> Segments { get; }

    public int IgnoredEntityCount { get; }

    public int SampledCurveSegmentCount { get; }
}
