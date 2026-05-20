using OpenCad2D.Core.Editing.Curves;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Represents a shared geometric intersection point together with native parameters on both entities.
/// </summary>
/// <param name="Point">Shared geometric point that must be reused by explicit-vertex entities.</param>
/// <param name="FirstParameter">Native parameter of the point on the first entity.</param>
/// <param name="SecondParameter">Native parameter of the point on the second entity.</param>
/// <param name="Kind">Intersection classification used by editing commands.</param>
public readonly record struct CadIntersectionPoint(
    Point2D Point,
    double FirstParameter,
    double SecondParameter,
    CadIntersectionKind Kind)
{
    /// <summary>
    /// Creates a curve cut for the first entity using the shared intersection point.
    /// </summary>
    public CurveCut FirstCut => new(FirstParameter, Point);

    /// <summary>
    /// Creates a curve cut for the second entity using the shared intersection point.
    /// </summary>
    public CurveCut SecondCut => new(SecondParameter, Point);
}
