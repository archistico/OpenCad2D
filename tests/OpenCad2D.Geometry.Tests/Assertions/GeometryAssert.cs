using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests.Assertions;

public static class GeometryAssert
{
    public static void Equal(
        Point2D expected,
        Point2D actual,
        double tolerance = Tolerance.Default)
    {
        Assert.True(
            Tolerance.AreEqual(expected.X, actual.X, tolerance),
            $"Expected X = {expected.X}, actual X = {actual.X}.");

        Assert.True(
            Tolerance.AreEqual(expected.Y, actual.Y, tolerance),
            $"Expected Y = {expected.Y}, actual Y = {actual.Y}.");
    }

    public static void Equal(
        BoundingBox2D expected,
        BoundingBox2D actual,
        double tolerance = Tolerance.Default)
    {
        Assert.True(
            Tolerance.AreEqual(expected.MinX, actual.MinX, tolerance),
            $"Expected MinX = {expected.MinX}, actual MinX = {actual.MinX}.");

        Assert.True(
            Tolerance.AreEqual(expected.MinY, actual.MinY, tolerance),
            $"Expected MinY = {expected.MinY}, actual MinY = {actual.MinY}.");

        Assert.True(
            Tolerance.AreEqual(expected.MaxX, actual.MaxX, tolerance),
            $"Expected MaxX = {expected.MaxX}, actual MaxX = {actual.MaxX}.");

        Assert.True(
            Tolerance.AreEqual(expected.MaxY, actual.MaxY, tolerance),
            $"Expected MaxY = {expected.MaxY}, actual MaxY = {actual.MaxY}.");
    }

    public static void ContainsPoint(
        IEnumerable<Point2D> points,
        Point2D expected,
        double tolerance = Tolerance.Default)
    {
        bool exists = points.Any(point =>
            Tolerance.AreEqual(point.X, expected.X, tolerance) &&
            Tolerance.AreEqual(point.Y, expected.Y, tolerance));

        Assert.True(
            exists,
            $"Expected collection to contain point ({expected.X}, {expected.Y}).");
    }
}