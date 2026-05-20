using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing.Curves;

/// <summary>
/// Adapts a CAD entity to a native parametric curve used by TRIM/BREAK-style operations.
/// </summary>
public interface ICurveAdapter
{
    CadEntity Source { get; }

    bool IsClosed { get; }

    double StartParameter { get; }

    double EndParameter { get; }

    double Period { get; }

    Point2D PointAt(double parameter);

    bool TryProjectPointToCut(
        Point2D point,
        GeometryTolerance tolerance,
        out CurveCut cut);

    IReadOnlyList<CadEntity> BuildFragments(
        IReadOnlyList<CurveInterval> intervalsToKeep,
        GeometryTolerance tolerance);
}
