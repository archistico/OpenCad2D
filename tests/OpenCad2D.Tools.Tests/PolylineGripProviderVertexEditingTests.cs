using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Grips;

namespace OpenCad2D.Tools.Tests;

public sealed class PolylineGripProviderVertexEditingTests
{
    [Fact]
    public void GetGrips_ForOpenGenericPolyline_ShouldReturnInsertGripsAtSegmentMidpoints()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        IReadOnlyList<GripPoint> grips = provider.GetGrips(polyline);

        Assert.Equal(6, grips.Count);
        Assert.Equal(GripKind.MoveVertex, grips[0].Kind);
        Assert.Equal(GripKind.MoveVertex, grips[1].Kind);
        Assert.Equal(GripKind.MoveVertex, grips[2].Kind);
        Assert.Equal(GripKind.InsertVertex, grips[3].Kind);
        Assert.Equal(new Point2D(5, 0), grips[3].Position);
        Assert.Equal(GripKind.InsertVertex, grips[4].Kind);
        Assert.Equal(new Point2D(10, 5), grips[4].Position);
        Assert.Equal(GripKind.MoveEntity, grips[5].Kind);
    }

    [Fact]
    public void GetGrips_ForClosedGenericPolyline_ShouldReturnInsertGripForClosingSegment()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, 10)
        }, isClosed: true);

        IReadOnlyList<GripPoint> grips = provider.GetGrips(polyline);

        Assert.Equal(7, grips.Count);
        Assert.Equal(GripKind.InsertVertex, grips[3].Kind);
        Assert.Equal(GripKind.InsertVertex, grips[4].Kind);
        Assert.Equal(GripKind.InsertVertex, grips[5].Kind);
        Assert.Equal(new Point2D(2.5, 5), grips[5].Position);
        Assert.Equal(GripKind.MoveEntity, grips[6].Kind);
    }

    [Fact]
    public void ApplyGripMove_ForInsertGrip_ShouldInsertVertexAtDestination()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        GripPoint insertGrip = provider.GetGrips(polyline)
            .Single(grip => grip.Kind == GripKind.InsertVertex && grip.Position == new Point2D(5, 0));

        var result = (PolylineEntity)provider.ApplyGripMove(
            polyline,
            insertGrip.GripIndex,
            new Point2D(4, 2));

        Assert.Equal(polyline.Id, result.Id);
        Assert.Equal(4, result.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), result.Vertices[0]);
        Assert.Equal(new Point2D(4, 2), result.Vertices[1]);
        Assert.Equal(new Point2D(10, 0), result.Vertices[2]);
        Assert.Equal(new Point2D(10, 10), result.Vertices[3]);
    }

    [Fact]
    public void InsertVertex_ForClosedPolylineClosingSegment_ShouldAppendBeforeClosure()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, 10)
        }, isClosed: true);
        GripPoint closingSegmentGrip = provider.GetGrips(polyline)
            .Where(grip => grip.Kind == GripKind.InsertVertex)
            .Last();

        var result = (PolylineEntity)provider.InsertVertex(
            polyline,
            closingSegmentGrip.GripIndex,
            new Point2D(2, 6));

        Assert.True(result.IsClosed);
        Assert.Equal(4, result.Vertices.Count);
        Assert.Equal(new Point2D(2, 6), result.Vertices[3]);
    }

    [Fact]
    public void DeleteVertex_ForOpenPolyline_ShouldRemoveVertexAndPreserveId()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });

        var result = (PolylineEntity)provider.DeleteVertex(
            polyline,
            gripIndex: 1);

        Assert.Equal(polyline.Id, result.Id);
        Assert.False(result.IsClosed);
        Assert.Equal(2, result.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), result.Vertices[0]);
        Assert.Equal(new Point2D(10, 10), result.Vertices[1]);
    }

    [Fact]
    public void DeleteVertex_ForClosedPolyline_ShouldKeepClosedFlag()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(8, 10),
            new Point2D(0, 10)
        }, isClosed: true);

        var result = (PolylineEntity)provider.DeleteVertex(
            polyline,
            gripIndex: 1);

        Assert.True(result.IsClosed);
        Assert.Equal(3, result.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), result.Vertices[0]);
        Assert.Equal(new Point2D(8, 10), result.Vertices[1]);
        Assert.Equal(new Point2D(0, 10), result.Vertices[2]);
    }

    [Fact]
    public void DeleteVertex_ForOpenPolylineWithTwoVertices_ShouldBeRejected()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });

        Assert.False(provider.CanDeleteVertex(polyline, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.DeleteVertex(polyline, 0));
    }

    [Fact]
    public void RectanglePolyline_ShouldKeepRectangleSpecificGripBehavior()
    {
        var provider = new PolylineGripProvider();
        var rectangle = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(0, 5)
        }, isClosed: true);

        IReadOnlyList<GripPoint> grips = provider.GetGrips(rectangle);

        Assert.Equal(9, grips.Count);
        Assert.DoesNotContain(grips, grip => grip.Kind == GripKind.InsertVertex);
        Assert.False(provider.CanDeleteVertex(rectangle, 0));
    }

    [Fact]
    public void GetGrips_ForPolylineWithBulge_ShouldPlaceInsertGripOnArcSegment()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { 1.0 });

        GripPoint insertGrip = provider.GetGrips(polyline)
            .Single(grip => grip.Kind == GripKind.InsertVertex);

        Assert.Equal(5, insertGrip.Position.X, precision: 10);
        Assert.Equal(5, insertGrip.Position.Y, precision: 10);
    }


    [Fact]
    public void GetGrips_ForPolylineWithBulge_ShouldExposeArcShapeGrip()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { 1.0 });

        GripPoint shapeGrip = provider.GetGrips(polyline)
            .Single(grip => grip.Kind == GripKind.ResizeRadius);

        Assert.Equal(5, shapeGrip.Position.X, precision: 10);
        Assert.Equal(5, shapeGrip.Position.Y, precision: 10);
    }

    [Fact]
    public void ApplyGripMove_ForPolylineArcShapeGrip_ShouldUpdateBulgeFromThreePointArc()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { 1.0 });
        GripPoint shapeGrip = provider.GetGrips(polyline)
            .Single(grip => grip.Kind == GripKind.ResizeRadius);

        var result = (PolylineEntity)provider.ApplyGripMove(
            polyline,
            shapeGrip.GripIndex,
            new Point2D(5, 2.5));

        Assert.Equal(polyline.Id, result.Id);
        Assert.Single(result.SegmentBulges);
        Assert.NotEqual(1.0, result.SegmentBulges[0]);
        Assert.True(result.HasArcSegments);
    }

    [Fact]
    public void ApplyGripMove_ForPolylineArcShapeGripOntoChord_ShouldFlattenSegment()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { 1.0 });
        GripPoint shapeGrip = provider.GetGrips(polyline)
            .Single(grip => grip.Kind == GripKind.ResizeRadius);

        var result = (PolylineEntity)provider.ApplyGripMove(
            polyline,
            shapeGrip.GripIndex,
            new Point2D(5, 0));

        Assert.Equal(0.0, Assert.Single(result.SegmentBulges), precision: 12);
        Assert.False(result.HasArcSegments);
    }

    [Fact]
    public void ApplyGripMove_ForPolylineWithBulge_ShouldPreserveBulgesWhenMovingVertex()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { 1.0, 0.0 });

        var result = (PolylineEntity)provider.ApplyGripMove(
            polyline,
            gripIndex: 1,
            destination: new Point2D(10, 2));

        Assert.Equal(new[] { 1.0, 0.0 }, result.SegmentBulges);
        Assert.Equal(new Point2D(10, 2), result.Vertices[1]);
    }

    [Fact]
    public void ApplyGripMove_ForPolylineWithBulge_ShouldPreserveBulgesWhenMovingEntity()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { 1.0, -0.5 });

        int moveEntityGripIndex = polyline.Vertices.Count;
        var result = (PolylineEntity)provider.ApplyGripMove(
            polyline,
            moveEntityGripIndex,
            new Point2D(20, 10));

        Assert.Equal(new[] { 1.0, -0.5 }, result.SegmentBulges);
    }

    [Fact]
    public void InsertVertex_ForPolylineWithBulge_ShouldKeepValidBulgeCountAndFlattenSplitSegment()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { 1.0, -0.5 });
        GripPoint arcInsertGrip = provider.GetGrips(polyline)
            .First(grip => grip.Kind == GripKind.InsertVertex);

        var result = (PolylineEntity)provider.InsertVertex(
            polyline,
            arcInsertGrip.GripIndex,
            new Point2D(5, 4));

        Assert.Equal(4, result.Vertices.Count);
        Assert.Equal(new[] { 0.0, 0.0, -0.5 }, result.SegmentBulges);
    }

    [Fact]
    public void DeleteVertex_ForPolylineWithBulge_ShouldKeepValidBulgeCountAndFlattenMergedSegment()
    {
        var provider = new PolylineGripProvider();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0),
                new Point2D(30, 0)
            },
            segmentBulges: new[] { 1.0, -0.5, 0.25 });

        var result = (PolylineEntity)provider.DeleteVertex(
            polyline,
            gripIndex: 1);

        Assert.Equal(3, result.Vertices.Count);
        Assert.Equal(new[] { 0.0, 0.25 }, result.SegmentBulges);
    }

}
