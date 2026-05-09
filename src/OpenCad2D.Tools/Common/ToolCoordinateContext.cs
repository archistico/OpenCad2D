using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Coordinates;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides coordinate systems and geometric precision settings for CAD tools.
/// </summary>
public sealed class ToolCoordinateContext
{
    public ToolCoordinateContext(
        CoordinateSystem2D currentUcs,
        GeometryTolerance geometryTolerance)
    {
        CurrentUcs = currentUcs;
        GeometryTolerance = geometryTolerance;
    }

    public CoordinateSystem2D CurrentUcs { get; set; }

    public GeometryTolerance GeometryTolerance { get; set; }
}