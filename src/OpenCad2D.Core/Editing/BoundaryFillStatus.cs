namespace OpenCad2D.Core.Editing;

/// <summary>
/// Machine-readable status produced by a boundary fill search.
/// </summary>
public enum BoundaryFillStatus
{
    Success,
    UnsupportedOnly,
    NoUsableSegments,
    NoClosedBoundary,
    DegenerateBoundary
}
