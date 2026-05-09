using OpenCad2D.Geometry.Coordinates;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Tests;

public sealed class CoordinateSystem2DTests
{
    [Fact]
    public void WorldToUser_ShouldTranslateOrigin()
    {
        CoordinateSystem2D ucs = new(
            new Point2D(100, 50),
            new Vector2D(1, 0));

        Point2D userPoint = ucs.WorldToUser(
            new Point2D(110, 70));

        Assert.Equal(10, userPoint.X, 9);
        Assert.Equal(20, userPoint.Y, 9);
    }

    [Fact]
    public void UserToWorld_ShouldTranslateOrigin()
    {
        CoordinateSystem2D ucs = new(
            new Point2D(100, 50),
            new Vector2D(1, 0));

        Point2D worldPoint = ucs.UserToWorld(
            new Point2D(10, 20));

        Assert.Equal(110, worldPoint.X, 9);
        Assert.Equal(70, worldPoint.Y, 9);
    }

    [Fact]
    public void WorldToUser_ShouldRespectRotation()
    {
        CoordinateSystem2D ucs = CoordinateSystem2D.FromOriginAndAngle(
            Point2D.Origin,
            Math.PI / 2);

        Point2D userPoint = ucs.WorldToUser(
            new Point2D(0, 10));

        Assert.Equal(10, userPoint.X, 9);
        Assert.Equal(0, userPoint.Y, 9);
    }

    [Fact]
    public void UserToWorld_ShouldRespectRotation()
    {
        CoordinateSystem2D ucs = CoordinateSystem2D.FromOriginAndAngle(
            Point2D.Origin,
            Math.PI / 2);

        Point2D worldPoint = ucs.UserToWorld(
            new Point2D(10, 0));

        Assert.Equal(0, worldPoint.X, 9);
        Assert.Equal(10, worldPoint.Y, 9);
    }
}