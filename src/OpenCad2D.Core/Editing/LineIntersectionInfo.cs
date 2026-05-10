using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

public readonly record struct LineIntersectionInfo(
    Point2D Point,
    double FirstParameter,
    double SecondParameter);
