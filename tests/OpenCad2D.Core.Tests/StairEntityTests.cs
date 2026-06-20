using OpenCad2D.Core.Architecture.Stairs;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class StairEntityTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.Plan,
            width: 1.2,
            treadCount: 4,
            treadDepth: 0.30,
            riserHeight: 0.17);

        Assert.Equal(EntityKind.Stair, stair.Kind);
        Assert.NotEqual(EntityId.Empty, stair.Id);
        Assert.Equal(LayerId.Default, stair.LayerId);
        Assert.True(stair.IsVisible);
        Assert.False(stair.IsLocked);
        Assert.Equal(1.20, stair.TotalRun, precision: 10);
        Assert.Equal(0.68, stair.TotalRise, precision: 10);
    }

    [Fact]
    public void Constructor_WithInvalidParameters_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StairEntity(
            Point2D.Origin,
            StairViewKind.Plan,
            width: 0,
            treadCount: 4,
            treadDepth: 0.30,
            riserHeight: 0.17));

        Assert.Throws<ArgumentOutOfRangeException>(() => new StairEntity(
            Point2D.Origin,
            StairViewKind.Plan,
            width: 1.2,
            treadCount: 0,
            treadDepth: 0.30,
            riserHeight: 0.17));

        Assert.Throws<ArgumentException>(() => new StairEntity(
            Point2D.Origin,
            StairViewKind.Plan,
            width: 1.2,
            treadCount: 4,
            treadDepth: 0.30,
            riserHeight: 0.17,
            xAxis: new Vector2D(1, 0),
            yAxis: new Vector2D(2, 0)));
    }

    [Fact]
    public void PlanGeometry_ShouldCreateBoundaryAndTreadLines()
    {
        var stair = new StairEntity(
            new Point2D(10, 20),
            StairViewKind.Plan,
            width: 1.0,
            treadCount: 4,
            treadDepth: 0.25,
            riserHeight: 0.17);

        StairGeometry geometry = stair.GetGeneratedGeometry();

        Assert.Equal(7, geometry.Segments.Count);
        Assert.Contains(geometry.Segments, segment =>
            segment.Start == new Point2D(10.25, 20)
            && segment.End == new Point2D(10.25, 21));
        Assert.Contains(geometry.Segments, segment =>
            segment.Start == new Point2D(10.75, 20)
            && segment.End == new Point2D(10.75, 21));
    }

    [Fact]
    public void PlanBoundingBox_ShouldMatchRunAndWidth()
    {
        var stair = new StairEntity(
            new Point2D(2, 3),
            StairViewKind.Plan,
            width: 1.1,
            treadCount: 5,
            treadDepth: 0.28,
            riserHeight: 0.17);

        BoundingBox2D box = stair.GetBoundingBox();

        Assert.Equal(2, box.MinX, precision: 10);
        Assert.Equal(3, box.MinY, precision: 10);
        Assert.Equal(3.4, box.MaxX, precision: 10);
        Assert.Equal(4.1, box.MaxY, precision: 10);
    }

    [Fact]
    public void SideElevationGeometry_ShouldCreateStepProfileAndStructureLine()
    {
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.SideElevation,
            width: 1.0,
            treadCount: 3,
            treadDepth: 0.30,
            riserHeight: 0.17,
            showStructure: true,
            slabThickness: 0.25);

        StairGeometry geometry = stair.GetGeneratedGeometry();

        Assert.Equal(7, geometry.Segments.Count);
        Assert.Contains(geometry.Segments, segment =>
            ArePointsEqual(segment.Start, new Point2D(0, -0.25))
            && ArePointsEqual(segment.End, new Point2D(0.90, 0.26)));
    }

    [Fact]
    public void SideElevationBoundingBox_ShouldIncludeRunRiseAndStructure()
    {
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.SideElevation,
            width: 1.0,
            treadCount: 3,
            treadDepth: 0.30,
            riserHeight: 0.17,
            showStructure: true,
            slabThickness: 0.25);

        BoundingBox2D box = stair.GetBoundingBox();

        Assert.Equal(0, box.MinX, precision: 10);
        Assert.Equal(-0.25, box.MinY, precision: 10);
        Assert.Equal(0.90, box.MaxX, precision: 10);
        Assert.Equal(0.51, box.MaxY, precision: 10);
    }

    [Fact]
    public void FrontElevationGeometry_ShouldRepresentWidthAndTotalRise()
    {
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.FrontElevation,
            width: 1.2,
            treadCount: 4,
            treadDepth: 0.30,
            riserHeight: 0.17);

        StairGeometry geometry = stair.GetGeneratedGeometry();
        BoundingBox2D box = stair.GetBoundingBox();

        Assert.Equal(7, geometry.Segments.Count);
        Assert.Equal(1.2, box.Width, precision: 10);
        Assert.Equal(0.68, box.Height, precision: 10);
    }

    [Fact]
    public void Transform_WithTranslation_ShouldMoveGeneratedGeometryWithSameId()
    {
        var id = EntityId.New();
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.Plan,
            width: 1.0,
            treadCount: 2,
            treadDepth: 0.25,
            riserHeight: 0.17,
            id: id);

        var moved = Assert.IsType<StairEntity>(
            stair.Transform(Matrix2D.Translation(5, 10)));

        Assert.Equal(id, moved.Id);
        Assert.Equal(new Point2D(5, 10), moved.InsertionPoint);
        AssertPoint(new Point2D(5.25, 10.5), moved.GetBoundingBox().Center);
    }

    [Fact]
    public void Transform_WithUniformScale_ShouldScaleParameters()
    {
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.Plan,
            width: 1.0,
            treadCount: 2,
            treadDepth: 0.25,
            riserHeight: 0.17);

        var scaled = Assert.IsType<StairEntity>(
            stair.Transform(Matrix2D.Scale(2.0, Point2D.Origin)));

        Assert.Equal(2.0, scaled.Width, precision: 10);
        Assert.Equal(0.50, scaled.TreadDepth, precision: 10);
        Assert.Equal(0.34, scaled.RiserHeight, precision: 10);
        Assert.Equal(1.0, scaled.TotalRun, precision: 10);
    }

    [Fact]
    public void DistanceTo_ShouldUseGeneratedLinework()
    {
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.Plan,
            width: 1.0,
            treadCount: 2,
            treadDepth: 0.5,
            riserHeight: 0.17);

        double distance = stair.DistanceTo(new Point2D(0.5, 1.25));

        Assert.Equal(0.25, distance, precision: 10);
    }

    [Fact]
    public void WithLayer_ShouldKeepStairParameters()
    {
        var stair = new StairEntity(
            Point2D.Origin,
            StairViewKind.SideElevation,
            width: 1.0,
            treadCount: 3,
            treadDepth: 0.30,
            riserHeight: 0.17,
            showStructure: true);

        var moved = Assert.IsType<StairEntity>(stair.WithLayer(new LayerId("Stairs")));

        Assert.Equal(new LayerId("Stairs"), moved.LayerId);
        Assert.Equal(stair.ViewKind, moved.ViewKind);
        Assert.Equal(stair.Width, moved.Width);
        Assert.Equal(stair.TreadCount, moved.TreadCount);
        Assert.Equal(stair.TreadDepth, moved.TreadDepth);
        Assert.Equal(stair.RiserHeight, moved.RiserHeight);
        Assert.True(moved.ShowStructure);
    }

    private static bool ArePointsEqual(Point2D actual, Point2D expected)
    {
        return Math.Abs(actual.X - expected.X) <= 1e-10
            && Math.Abs(actual.Y - expected.Y) <= 1e-10;
    }

    private static void AssertPoint(Point2D expected, Point2D actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 10);
        Assert.Equal(expected.Y, actual.Y, precision: 10);
    }
}
