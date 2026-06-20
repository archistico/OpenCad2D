namespace OpenCad2D.Core.Editing;

/// <summary>
/// High-level outcome of a Boundary Fill detection attempt.
/// </summary>
public enum BoundaryFillStatus
{
    Success,
    UnsupportedOnly,
    NoUsableSegments,
    NoClosedBoundary,
    DegenerateBoundary
}
