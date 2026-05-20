namespace OpenCad2D.Core.Editing;

/// <summary>
/// Classifies an entity intersection for CAD editing operations.
/// </summary>
public enum CadIntersectionKind
{
    /// <summary>
    /// The entities cross at an interior point of both editable curves.
    /// </summary>
    Crossing,

    /// <summary>
    /// The intersection lies on at least one editable curve endpoint.
    /// </summary>
    Endpoint,

    /// <summary>
    /// The entities touch tangentially at the intersection point.
    /// </summary>
    Tangent,

    /// <summary>
    /// The entities overlap along a continuous segment or curve interval.
    /// </summary>
    Overlap
}
