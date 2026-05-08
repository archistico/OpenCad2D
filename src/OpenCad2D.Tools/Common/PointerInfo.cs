using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Represents pointer input in model coordinates.
/// </summary>
public sealed class PointerInfo
{
    public PointerInfo(Point2D modelPoint)
    {
        ModelPoint = modelPoint;
    }

    public Point2D ModelPoint { get; }
}