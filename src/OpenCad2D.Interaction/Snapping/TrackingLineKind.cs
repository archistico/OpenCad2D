namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Defines the kind of temporary construction line generated from a smart point.
/// </summary>
public enum TrackingLineKind
{
    Horizontal,
    Vertical,

    /// <summary>
    /// Temporary line generated from the real direction of a linear entity or a straight polyline segment.
    /// </summary>
    EntityExtension
}
