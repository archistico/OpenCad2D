namespace OpenCad2D.Core.Editing;

/// <summary>
/// Diagnostic counters produced by a boundary fill search.
/// They are intentionally lightweight so the tool can use them for HUD/status messages later.
/// </summary>
public sealed record BoundaryFillDiagnostics(
    int SourceSegmentCount,
    int GraphEdgeCount,
    int CandidateFaceCount,
    int IgnoredEntityCount,
    int BridgedGapCount,
    int SampledCurveSegmentCount,
    double GapTolerance)
{
    public static BoundaryFillDiagnostics Empty(double gapTolerance)
    {
        return new BoundaryFillDiagnostics(
            0,
            0,
            0,
            0,
            0,
            0,
            gapTolerance);
    }
}
