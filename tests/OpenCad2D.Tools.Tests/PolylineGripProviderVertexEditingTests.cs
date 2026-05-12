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
}
