using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadTrimServiceTests
{
    [Fact]
    public void TrimLine_ByCircleBoundary_ShouldKeepSideAwayFromPickedPoint()
    {
        var target = new LineEntity(new Point2D(-10, 0), new Point2D(10, 0));
        var boundary = new CircleEntity(new Point2D(0, 0), 5);

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(0, 0));

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.IsType<LineEntity>(entity));
    }


    [Fact]
    public void TrimLine_ByBoundary_ShouldReuseSharedIntersectionPointAsEndpoint()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var boundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(8, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(0, 0), kept.Start);
        Assert.Equal(new Point2D(5, 0), kept.End);
    }

    [Fact]
    public void TrimTwoLinesMutually_ShouldCreateExactlyMatchingSharedEndpoint()
    {
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));

        LineEntity trimmedHorizontal = Assert.IsType<LineEntity>(Assert.Single(
            CadTrimService.TrimByBoundary(
                horizontal,
                vertical,
                new Point2D(8, 0))));

        LineEntity trimmedVertical = Assert.IsType<LineEntity>(Assert.Single(
            CadTrimService.TrimByBoundary(
                vertical,
                horizontal,
                new Point2D(5, -3))));

        Assert.Equal(trimmedHorizontal.End, trimmedVertical.Start);
        Assert.Equal(new Point2D(5, 0), trimmedHorizontal.End);
    }

    [Fact]
    public void TrimCircle_ByLineBoundary_ShouldCreateArcFragments()
    {
        var target = new CircleEntity(new Point2D(0, 0), 5);
        var boundary = new LineEntity(new Point2D(0, -10), new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        ArcEntity arc = Assert.Single(result.OfType<ArcEntity>());

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.Radius, arc.Radius);
    }

    [Fact]
    public void TrimArc_ByLineBoundary_ShouldCreateArcFragment()
    {
        var target = new ArcEntity(
            new Point2D(0, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));
        var boundary = new LineEntity(new Point2D(0, -10), new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(4, 3));

        ArcEntity arc = Assert.Single(result.OfType<ArcEntity>());

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.Radius, arc.Radius);
    }

    [Fact]
    public void TrimLine_ByOverlappingLineBoundary_ShouldUseOverlapBoundaryCut()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var boundary = new LineEntity(new Point2D(5, 0), new Point2D(15, 0));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(7, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(0, 0), kept.Start);
        Assert.Equal(new Point2D(5, 0), kept.End);
    }

    [Fact]
    public void TrimCircle_ByOverlappingArcBoundary_ShouldUseArcEndpointCuts()
    {
        var target = new CircleEntity(new Point2D(0, 0), 10);
        var boundary = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            PointOnCircle(10, 45));

        ArcEntity kept = Assert.IsType<ArcEntity>(Assert.Single(result));

        Assert.True(kept.Geometry.ContainsPoint(PointOnCircle(10, 180)));
        Assert.False(kept.Geometry.ContainsPoint(PointOnCircle(10, 45)));
    }

    [Fact]
    public void TrimArc_ByOverlappingArcBoundary_ShouldUseOverlapBoundaryCut()
    {
        var target = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));
        var boundary = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(90),
            Angle.FromDegrees(270));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            PointOnCircle(10, 135));

        ArcEntity kept = Assert.IsType<ArcEntity>(Assert.Single(result));

        Assert.True(kept.Geometry.ContainsPoint(PointOnCircle(10, 45)));
        Assert.False(kept.Geometry.ContainsPoint(PointOnCircle(10, 135)));
    }

    [Fact]
    public void TrimPolyline_ByLineBoundary_ShouldCreatePolylineFragments()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        var boundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(8, 0));

        PolylineEntity polyline = Assert.Single(result.OfType<PolylineEntity>());

        Assert.Equal(new Point2D(0, 0), polyline.Vertices[0]);
        Assert.Equal(new Point2D(5, 0), polyline.Vertices[^1]);
    }


    [Fact]
    public void TrimOpenPolyline_WithBoundaryAtStartEndpoint_ShouldIgnoreEndpointIntersection()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        var boundary = new LineEntity(new Point2D(0, -5), new Point2D(0, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        Assert.Empty(result);
    }

    [Fact]
    public void TrimOpenPolyline_WithBoundaryAtEndEndpoint_ShouldIgnoreEndpointIntersection()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        var boundary = new LineEntity(new Point2D(10, -5), new Point2D(10, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(5, 0));

        Assert.Empty(result);
    }

    [Fact]
    public void TrimOpenPolyline_ByLineBoundaryOnSecondSegment_ShouldCreatePolylineFragments()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        var boundary = new LineEntity(new Point2D(5, 5), new Point2D(15, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(10, 8));

        PolylineEntity kept = Assert.IsType<PolylineEntity>(Assert.Single(result));

        Assert.False(kept.IsClosed);
        Assert.Equal(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5)
            },
            kept.Vertices);
    }

    [Fact]
    public void TrimClosedPolylinePolygon_ByLineBoundary_ShouldCreateOpenPolylineFragments()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        }, isClosed: true);
        var boundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 15));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(10, 5));

        Assert.NotEmpty(result);
        Assert.All(result, entity => Assert.IsType<PolylineEntity>(entity));
        Assert.All(result.OfType<PolylineEntity>(), polyline => Assert.False(polyline.IsClosed));
        Assert.Contains(result.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Contains(new Point2D(5, 0)));
        Assert.Contains(result.OfType<PolylineEntity>(), polyline =>
            polyline.Vertices.Contains(new Point2D(5, 10)));
    }

    [Fact]
    public void TrimPolyline_ByTwoBoundariesOnSameSegment_ShouldRemovePickedInterval()
    {
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(20, 0)
        });
        var leftBoundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));
        var rightBoundary = new LineEntity(new Point2D(15, -5), new Point2D(15, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(10, 0));

        Assert.Equal(2, result.Count);
        Assert.Contains(
            result.OfType<PolylineEntity>(),
            polyline => polyline.Vertices.SequenceEqual(new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 0)
            }));
        Assert.Contains(
            result.OfType<PolylineEntity>(),
            polyline => polyline.Vertices.SequenceEqual(new[]
            {
                new Point2D(15, 0),
                new Point2D(20, 0)
            }));
    }

    [Fact]
    public void TrimEllipse_ByLineBoundary_ShouldCreateNativeEllipticalArcFragment()
    {
        var target = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var boundary = new LineEntity(new Point2D(0, -10), new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(10, 0));

        EllipticalArcEntity arc = Assert.IsType<EllipticalArcEntity>(Assert.Single(result));

        Assert.True(arc.StartPoint.DistanceTo(new Point2D(0, 5)) <= 1.0e-6);
        Assert.True(arc.EndPoint.DistanceTo(new Point2D(0, -5)) <= 1.0e-6);
        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.MajorAxis, arc.MajorAxis);
        Assert.Equal(target.MinorRadius, arc.MinorRadius);
    }

    [Fact]
    public void TrimBezierSpline_ByLineBoundary_ShouldCreateNativeBezierFragments()
    {
        var target = new BezierSplineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 10),
            new Point2D(10, 0)
        });
        var boundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 15));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(3, 4));

        BezierSplineEntity fragment = Assert.IsType<BezierSplineEntity>(Assert.Single(result));

        Assert.Equal(3, fragment.ControlPoints.Count);
        Assert.True(fragment.ControlPoints[0].DistanceTo(new Point2D(5, 5)) <= 1.0e-6);
        Assert.True(fragment.ControlPoints[^1].DistanceTo(new Point2D(10, 0)) <= 1.0e-6);
    }

    [Fact]
    public void TrimBezierSpline_ByLineBoundary_ShouldNotCreatePolylineEntity()
    {
        var target = new BezierSplineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 10),
            new Point2D(10, 0)
        });
        var boundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 15));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(3, 4));

        Assert.NotEmpty(result);
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
    }

    private static Point2D PointOnCircle(double radius, double degrees)
    {
        double radians = degrees * Math.PI / 180.0;

        return new Point2D(
            Math.Cos(radians) * radius,
            Math.Sin(radians) * radius);
    }
}

public sealed class CadTrimServiceTwoBoundaryTests
{
    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenPickedMiddle_ShouldReturnTwoOuterFragments()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var leftBoundary = new LineEntity(new Point2D(3, -5), new Point2D(3, 5));
        var rightBoundary = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(5, 0));

        Assert.Equal(2, result.Count);

        LineEntity first = Assert.IsType<LineEntity>(result[0]);
        LineEntity second = Assert.IsType<LineEntity>(result[1]);

        Assert.Equal(new Point2D(0, 0), first.Start);
        Assert.Equal(new Point2D(3, 0), first.End);
        Assert.Equal(new Point2D(7, 0), second.Start);
        Assert.Equal(new Point2D(10, 0), second.End);
    }

    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenPickedLeftOuterSide_ShouldReturnSingleRightFragment()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var leftBoundary = new LineEntity(new Point2D(3, -5), new Point2D(3, 5));
        var rightBoundary = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(1, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(3, 0), kept.Start);
        Assert.Equal(new Point2D(10, 0), kept.End);
    }

    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenPickedRightOuterSide_ShouldReturnSingleLeftFragment()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var leftBoundary = new LineEntity(new Point2D(3, -5), new Point2D(3, 5));
        var rightBoundary = new LineEntity(new Point2D(7, -5), new Point2D(7, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(9, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(0, 0), kept.Start);
        Assert.Equal(new Point2D(7, 0), kept.End);
    }

    [Fact]
    public void TrimLine_ByTwoBoundaries_WhenOnlyOneBoundaryIntersects_ShouldBehaveLikeSingleBoundaryTrim()
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var intersectingBoundary = new LineEntity(new Point2D(5, -5), new Point2D(5, 5));
        var externalBoundary = new LineEntity(new Point2D(20, -5), new Point2D(20, 5));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { intersectingBoundary, externalBoundary },
            new Point2D(8, 0));

        LineEntity kept = Assert.IsType<LineEntity>(Assert.Single(result));

        Assert.Equal(new Point2D(0, 0), kept.Start);
        Assert.Equal(new Point2D(5, 0), kept.End);
    }
}

public sealed class CadTrimServiceCurveSplitPipelineTests
{
    [Fact]
    public void TrimCircle_ByTwoLineBoundaries_ShouldRemovePickedIntervalAndKeepNativeArcs()
    {
        var target = new CircleEntity(new Point2D(0, 0), 10);
        var leftBoundary = new LineEntity(new Point2D(-5, -20), new Point2D(-5, 20));
        var rightBoundary = new LineEntity(new Point2D(5, -20), new Point2D(5, 20));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { leftBoundary, rightBoundary },
            new Point2D(0, 10));

        Assert.Equal(3, result.Count);
        Assert.All(result, entity => Assert.IsType<ArcEntity>(entity));
        Assert.All(result.OfType<ArcEntity>(), arc =>
        {
            Assert.Equal(target.Center, arc.Center);
            Assert.Equal(target.Radius, arc.Radius);
        });
    }

    [Fact]
    public void TrimArc_ByTwoLineBoundaries_ShouldRemovePickedIntervalAndKeepNativeArcs()
    {
        var target = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));
        var rightBoundary = new LineEntity(new Point2D(5, -20), new Point2D(5, 20));
        var leftBoundary = new LineEntity(new Point2D(-5, -20), new Point2D(-5, 20));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundaries(
            target,
            new CadEntity[] { rightBoundary, leftBoundary },
            new Point2D(0, 10));

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.IsType<ArcEntity>(entity));
        Assert.All(result.OfType<ArcEntity>(), arc =>
        {
            Assert.Equal(target.Center, arc.Center);
            Assert.Equal(target.Radius, arc.Radius);
        });
    }
    [Fact]
    public void TrimEllipse_ByLineBoundary_ShouldCreateNativeEllipticalArcFragment()
    {
        var target = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);
        var boundary = new LineEntity(
            new Point2D(0, -10),
            new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(10, 0));

        EllipticalArcEntity arc = Assert.Single(result.OfType<EllipticalArcEntity>());

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.MajorAxis, arc.MajorAxis);
        Assert.Equal(target.MinorRadius, arc.MinorRadius);
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
    }

    [Fact]
    public void TrimEllipticalArc_ByLineBoundary_ShouldCreateNativeEllipticalArcFragments()
    {
        var target = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5,
            0,
            Math.PI);
        var boundary = new LineEntity(
            new Point2D(0, -10),
            new Point2D(0, 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(5, 4));

        EllipticalArcEntity arc = Assert.Single(result.OfType<EllipticalArcEntity>());

        Assert.Equal(target.Center, arc.Center);
        Assert.Equal(target.MajorAxis, arc.MajorAxis);
        Assert.Equal(target.MinorRadius, arc.MinorRadius);
        Assert.DoesNotContain(result, entity => entity is PolylineEntity);
    }




    [Fact]
    public void TrimBulgedPolyline_ByLineBoundary_ShouldPreserveArcBulgeFragment()
    {
        double bulge = Math.Tan(Math.PI / 8.0);
        var target = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { bulge, 0.0 });
        Point2D arcMidpoint = target.GetSegmentMidpoints()[0];
        var boundary = new LineEntity(
            new Point2D(arcMidpoint.X, arcMidpoint.Y - 10),
            new Point2D(arcMidpoint.X, arcMidpoint.Y + 10));

        IReadOnlyList<CadEntity> result = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(0, 0));

        PolylineEntity kept = Assert.Single(result.OfType<PolylineEntity>());

        Assert.False(kept.IsClosed);
        Assert.Contains(kept.SegmentBulges, value => Math.Abs(value) > 1e-9);
    }

}
