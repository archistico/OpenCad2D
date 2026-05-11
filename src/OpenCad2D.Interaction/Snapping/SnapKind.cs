namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Defines the available object snap modes.
/// </summary>
[Flags]
public enum SnapKind
{
    None = 0,

    Endpoint = 1,

    Midpoint = 2,

    Center = 4,

    Quadrant = 8,

    Intersection = 16,

    Nearest = 32,

    Perpendicular = 64,

    Tangent = 128,

    Grid = 256,

    /// <summary>
    /// Entity snap used by selection-oriented tools.
    /// It is intentionally excluded from All because All represents geometric snaps.
    /// </summary>
    Entity = 512,

    All = Endpoint
        | Midpoint
        | Center
        | Quadrant
        | Intersection
        | Nearest
        | Perpendicular
        | Tangent
        | Grid,

    /// <summary>
    /// Snap set used when the active command needs to pick entities rather than points.
    /// </summary>
    EntityOnly = Entity
}