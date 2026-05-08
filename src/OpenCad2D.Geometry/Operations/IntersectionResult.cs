using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Operations;

public enum IntersectionKind
{
    None,
    Point,
    Overlapping
}

public readonly record struct IntersectionResult(
    IntersectionKind Kind,
    Point2D? Point = null)
{
    public bool HasIntersection => Kind != IntersectionKind.None;

    public static IntersectionResult None => new(IntersectionKind.None);

    public static IntersectionResult SinglePoint(Point2D point)
    {
        return new IntersectionResult(IntersectionKind.Point, point);
    }

    public static IntersectionResult Overlapping()
    {
        return new IntersectionResult(IntersectionKind.Overlapping);
    }
}