namespace OpenCad2D.Core.Anchors;

/// <summary>
/// Display and grid metadata for a canonical anchor point.
/// Row and column are UI-grid coordinates: row 0 is top, column 0 is left.
/// </summary>
public sealed record AnchorPointDescriptor(
    AnchorPoint Anchor,
    string Key,
    string DisplayName,
    int Row,
    int Column,
    int NumericShortcut);
