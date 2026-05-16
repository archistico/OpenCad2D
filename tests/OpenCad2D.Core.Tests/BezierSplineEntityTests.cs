using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class BezierSplineEntityTests
{
    [Fact]
    public void Constructor_ShouldStoreControlPointsAndClosedFlag()
    {
        var entity = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            },
            isClosed: true);

        Assert.Equal(EntityKind.BezierSpline, entity.Kind);
        Assert.True(entity.IsClosed);
        Assert.Equal(3, entity.ControlPoints.Count);
    }

    [Fact]
    public void Constructor_WithLessThanTwoControlPoints_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new BezierSplineEntity(
            new[] { new Point2D(0, 0) }));
    }

    [Fact]
    public void GetSamplePoints_ShouldIncludeStartAndEndForOpenSpline()
    {
        var entity = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            });

        IReadOnlyList<Point2D> samples = entity.GetSamplePoints(8);

        Assert.Equal(new Point2D(0, 0), samples[0]);
        Assert.Equal(new Point2D(10, 0), samples[^1]);
        Assert.Equal(9, samples.Count);
    }

    [Fact]
    public void Transform_ShouldMoveControlPoints()
    {
        var entity = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            },
            id: new EntityId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));

        BezierSplineEntity moved = Assert.IsType<BezierSplineEntity>(
            entity.Transform(Matrix2D.Translation(1, 2)));

        Assert.Equal(entity.Id, moved.Id);
        Assert.Equal(new Point2D(1, 2), moved.ControlPoints[0]);
        Assert.Equal(new Point2D(11, 2), moved.ControlPoints[^1]);
    }
}
