namespace OpenCad2D.Core.Editing;

/// <summary>
/// Diagnostic counters produced by Boundary Fill detection.
/// </summary>
public sealed class BoundaryFillDiagnostics
{
    public BoundaryFillDiagnostics(
        int sourceSegmentCount = 0,
        int graphEdgeCount = 0,
        int candidateFaceCount = 0,
        int ignoredEntityCount = 0,
        int bridgedGapCount = 0,
        int sampledCurveSegmentCount = 0,
        double gapTolerance = 0.0)
    {
        SourceSegmentCount = sourceSegmentCount;
        GraphEdgeCount = graphEdgeCount;
        CandidateFaceCount = candidateFaceCount;
        IgnoredEntityCount = ignoredEntityCount;
        BridgedGapCount = bridgedGapCount;
        SampledCurveSegmentCount = sampledCurveSegmentCount;
        GapTolerance = gapTolerance;
    }

    public static BoundaryFillDiagnostics Empty { get; } = new();

    public int SourceSegmentCount { get; }

    public int GraphEdgeCount { get; }

    public int CandidateFaceCount { get; }

    public int IgnoredEntityCount { get; }

    public int BridgedGapCount { get; }

    public int SampledCurveSegmentCount { get; }

    public double GapTolerance { get; }
}
