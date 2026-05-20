using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing.Curves;

/// <summary>
/// Represents a cut location on a native curve.
/// </summary>
/// <param name="Parameter">Native curve parameter used to rebuild the edited geometry.</param>
/// <param name="Point">Shared geometric point associated with the cut.</param>
public readonly record struct CurveCut(
    double Parameter,
    Point2D Point);
